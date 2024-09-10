import {
  type AssertionOptionsResponse,
  type VerifyAssertionResult,
  type AuthenticatorAssertionRawResponse,
  type AssertionResponseModel,
  type MakeAssertionRequestModel,
  type MakeCredentialsRequestModel,
  type GetExchangeCodeRequestModel,
  type CredentialOptionsModel,
  type DWT

} from './api/RelyingPartyClient'

import {
  AssertDefined,
  base64ToBase64url,
  concatUint8Arrays,
  generateRandomBuffer,
  getJWTParts,
  isMobile,
  isNullOrUndefined,
  sha256
} from './util'
import { relyingPartyClient } from './api'
import { makeWebauthnAssertion, makeWebauthnRegistration } from './webauthnClient'
import { generateX25519KeyPair } from './crypto'
import algosdk from 'algosdk'
import WebauthnLSIGWallet, { type Keypair } from './lsigWallet'

// These are the values for the integral client that we expect to be sound. Using placeholders for now, until the service is hosted.
export default class Argon {
  private readonly webauthnLSIGWallet: WebauthnLSIGWallet = new WebauthnLSIGWallet()
  private readonly clientId: string
  private readonly clientSecret: string
  private csk?: Keypair
  private jwt?: string
  private state?: string
  private codeVerifier?: string
  private redirectUri?: string
  private codeChallenge?: string
  private makeCredOptions?: CredentialOptionsModel

  constructor (clientId: string = '', clientSecret: string = '') {
    this.clientId = clientId
    this.clientSecret = clientSecret
  }

  public async initiateOAuth (): Promise<void> {
    // Initialize cryptographic variables for OAuth
    this.csk = await generateX25519KeyPair()
    this.state = Buffer.from(generateRandomBuffer(32)).toString('base64')
    this.codeVerifier = Buffer.from(generateRandomBuffer(32)).toString('base64')
    this.redirectUri = window.location.href
    const challenge = await sha256(Buffer.from(this.codeVerifier, 'base64'))
    this.codeChallenge = Buffer.from(challenge).toString('base64')
  }

  private async validateJWT (jwt: string, assertionResponse: AuthenticatorAssertionRawResponse, optionsDWT: DWT): Promise<void> {
    // Asserting that the claims weren't tampered with by a malicious MITM
    const dwt: DWT = getJWTParts(jwt).claims
    // compute the hash of the DWT
    const hashData = async (cspk: Uint8Array, exp: number, user: string, rand: string): Promise<string> => {
      // Concatenate all data into a single Uint8Array
      const data = concatUint8Arrays(
        cspk,
        algosdk.encodeUint64(exp),
        Buffer.from(user, 'utf8'),
        Buffer.from(rand, 'base64')
      )

      // Compute SHA-256 hash
      const hashBuffer = await crypto.subtle.digest('SHA-256', data)
      const asB64 = Buffer.from(hashBuffer).toString('base64')
      // Convert buffer to Uint8Array
      return asB64
    }
    AssertDefined(dwt.credSig, 'dwt.credSig must be defined')
    AssertDefined(dwt.authenticatorData, 'dwt.authenticatorData must be defined')
    AssertDefined(dwt.clientDataJSON, 'dwt.clientDataJSON must be defined')
    AssertDefined(assertionResponse.response, 'assertionResponse.response must be defined')
    AssertDefined(this.csk, 'this.csk must be defined')
    AssertDefined(optionsDWT.exp, 'optionsDWT.exp must be defined')
    AssertDefined(optionsDWT.user, 'optionsDWT.user must be defined')
    AssertDefined(optionsDWT.rand, 'optionsDWT.rand must be defined')
    const expectedHash = await hashData(this.csk.pk, optionsDWT.exp, optionsDWT.user, optionsDWT.rand)
    if (
      dwt.hash !== expectedHash ||
      base64ToBase64url(dwt.credSig) !== assertionResponse.response.signature ||
      base64ToBase64url(dwt.authenticatorData) !== assertionResponse.response.authenticatorData ||
      base64ToBase64url(dwt.clientDataJSON) !== assertionResponse.response.clientDataJSON ||
      dwt.cspk !== Buffer.from(this.csk.pk).toString('base64') ||
      dwt.exp !== optionsDWT.exp ||
      dwt.user !== optionsDWT.user ||
      dwt.rand !== optionsDWT.rand ||
      // exp is no more than 12 hours in the future
      dwt.exp > Math.floor(Date.now() / 1000) + 43200
    ) {
      throw new Error('JWT claims have been tampered with.')
    }
  }

  public async signIn (username: string): Promise<string> {
    await this.initiateOAuth()
    // Extract base64url encoded parameters for signing in
    AssertDefined(this.csk, 'this.csk must be defined')

    const cspkB64 = Buffer.from(this.csk.pk).toString('base64')

    const signInOptions = await relyingPartyClient.fidoAssertionOptionsPost({ username, userVerification: 'Required' }, cspkB64)
    const options: AssertionOptionsResponse = signInOptions.data
    AssertDefined(options.dwt, 'options.dwt must be defined')
    AssertDefined(options.fidoAssertionOptions, 'options.fidoAssertionOptions must be defined')
    AssertDefined(options.dwt.hash, 'options.dwt.hash must be defined')
    if (options.dwt.user !== username || options.dwt.cspk !== cspkB64) {
      throw new Error("DWT doesn't contain parameters that we sent to initialize it.")
    }

    if (options.fidoAssertionOptions.challenge !== base64ToBase64url(options.dwt.hash)) {
      throw new Error('Challenge being signed is not DWT as expected.')
    }

    const assertionOptions: AuthenticatorAssertionRawResponse = await makeWebauthnAssertion(options.fidoAssertionOptions)

    const makeAssertionRequest: MakeAssertionRequestModel = {
      clientResponse: assertionOptions,
      username,
      dwt: options.dwt,
      state: this.state,
      codeChallenge: this.codeChallenge,
      redirectUri: this.redirectUri
    }
    const response = await relyingPartyClient.fidoMakeAssertion(makeAssertionRequest)
    const authResponse: AssertionResponseModel = response.data
    const verifyAssertionResult: VerifyAssertionResult = authResponse.verifyAssertionResult!

    if (isNullOrUndefined(verifyAssertionResult?.credentialId)) {
      throw new Error('Credential ID undefined in RP auth response.')
    }
    AssertDefined(authResponse.code, 'authResponse.code must be defined')
    this.jwt = await this.exchangeCodeForJWT(authResponse.code)

    // validate the jwt to ensure no MITM attack
    await this.validateJWT(this.jwt, assertionOptions, options.dwt)

    return this.jwt
  }

  public async exchangeCodeForJWT (exchangeCode: string): Promise<string> {
    const body: GetExchangeCodeRequestModel = {
      code: exchangeCode,
      state: this.state,
      codeVerifier: this.codeVerifier,
      redirectUri: this.redirectUri,
      clientId: this.clientId,
      clientSecret: this.clientSecret
    }
    const exchangeCodeResponse = await relyingPartyClient.fidoExchangeCodeForJwt(body)
    return exchangeCodeResponse.data
  }

  public async signTransaction (tx: algosdk.Transaction): Promise<string> {
    AssertDefined(this.jwt, 'this.jwt must be defined')
    AssertDefined(this.csk, 'this.csk must be defined')
    await this.webauthnLSIGWallet.initialize(this.jwt, this.csk)
    return await this.webauthnLSIGWallet.sign(tx)
  }

  public async testTransaction (): Promise<void> {
    AssertDefined(this.jwt, 'this.jwt must be defined')
    AssertDefined(this.csk, 'this.csk must be defined')
    await this.webauthnLSIGWallet.initialize(this.jwt, this.csk)
    await this.webauthnLSIGWallet.testTransaction()
  }

  public async getEmailCodeIsValid (username: string, emailCode: string): Promise<boolean> {
    const response = await relyingPartyClient.fidoVerifyEmailCode({ email: username, code: emailCode })
    return response.data
  }

  public async initRegister (username: string): Promise<void> {
    await this.initiateOAuth()
    AssertDefined(this.csk, 'this.csk must be defined')
    const clientSessionPkB64 = Buffer.from(this.csk.pk).toString('base64')
    const options = await relyingPartyClient.fidoMakeCredentialOptions(
      {
        username,
        displayName: username,
        attType: 'none',
        authType: isMobile() ? 'platform' : 'platform',
        residentKey: 'required',
        userVerification: 'required'
      },
      clientSessionPkB64
    )
    this.makeCredOptions = options.data
  }

  public async jwtIsValid (jwt: string): Promise<boolean> {
    const response = await relyingPartyClient.fidoTokenIsValid(jwt)
    return response.data
  }

  public async register (username: string, emailCode: string): Promise<string> {
    AssertDefined(this.csk, 'this.csk must be defined')
    AssertDefined(this.makeCredOptions, 'this.makeCredOptions must be defined')
    AssertDefined(this.makeCredOptions.dwt, 'this.makeCredOptions.dwt must be defined')
    if (
      this.makeCredOptions.dwt.user !== username ||
      this.makeCredOptions.dwt.cspk !== Buffer.from(this.csk.pk).toString('base64')
    ) {
      throw new Error(
        "DWT doesn't contain parameters that we sent to initialize it."
      )
    }
    AssertDefined(this.makeCredOptions.options, 'this.makeCredOptions.options must be defined')
    AssertDefined(this.makeCredOptions.dwt.hash, 'this.makeCredOptions.dwt.hash must be defined')
    if (this.makeCredOptions.options.challenge !== base64ToBase64url(this.makeCredOptions.dwt.hash)) {
      throw new Error('Challenge being signed is not DWT as expected.')
    }
    AssertDefined(this.redirectUri, 'this.redirectUri must be defined')
    AssertDefined(this.state, 'this.state must be defined')
    AssertDefined(this.codeChallenge, 'this.codeChallenge must be defined')
    const makeCredentialsRequest: MakeCredentialsRequestModel =
      await makeWebauthnRegistration(
        this.makeCredOptions.options,
        this.makeCredOptions.dwt,
        this.redirectUri,
        this.state,
        this.codeChallenge,
        emailCode
      )
    const registerResponse = await relyingPartyClient.fidoMakeCredential(makeCredentialsRequest)
    const data = registerResponse.data
    if (isNullOrUndefined(data.fidoCredentialMakeResult)) {
      throw new Error('Credential ID undefined in RP auth registration response.')
    }
    AssertDefined(data.code, 'data.code must be defined')
    this.jwt = await this.exchangeCodeForJWT(data.code)
    return this.jwt
  }

  public async getAddress (): Promise<string> {
    AssertDefined(this.jwt, 'this.jwt must be defined')
    AssertDefined(this.csk, 'this.csk must be defined')
    await this.webauthnLSIGWallet.initialize(this.jwt, this.csk)
    return this.webauthnLSIGWallet.getAddress()
  }
}
