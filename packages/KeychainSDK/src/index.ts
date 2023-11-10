import {
  type MakeCredentialResponse,
  type AssertionOptionsResponse,
  type VerifyAssertionResult,
  type AuthenticatorAssertionRawResponse,
  type AssertionResponseModel,
  type MakeAssertionRequestModel,
  type DWT,
  type MakeCredentialsRequestModel
} from '@gradian/keychain-auth-server-client'
import {
  ed25519Sign,
  isMobile,
  isNullOrUndefined,
  verifyAssertionOptionsDWT
} from './util'

import { generateX25519KeyPair } from '@gradian/x25519ecdh'
import { type X25519Pair } from '@gradian/x25519ecdh/dist/src/types'
import { relyingPartyClient, webauthnLSIGClient } from './api'
import { makeWebauthnAssertion, makeWebauthnRegistration } from './webauthnClient'

export interface AssertionResponse {
  sessionKeyPK: string
  sessionKeySK?: string
  bearerToken: string
}
export async function signIn (username: string): Promise<AssertionResponse> {
  const clientSessionKeys: X25519Pair = generateX25519KeyPair()

  const csskB64 = Buffer.from(clientSessionKeys.priv).toString('base64')
  const cspkB64 = Buffer.from(clientSessionKeys.pub).toString('base64')

  const signInOptions = await relyingPartyClient.fidoAssertionOptionsPost(
    {
      username,
      userVerification: 'Required'
    },
    cspkB64
  )

  const options: AssertionOptionsResponse = signInOptions.data

  // Check cspk hasn't changed
  const dwt: DWT = options.dwt
  if (!verifyAssertionOptionsDWT(dwt, cspkB64, username)) {
    throw new Error("DWT doesn't contain parameters that we sent to initialize it.")
  }

  // Make assertion with webauthn (sign challenge with authenticator)
  const assertionOptions: AuthenticatorAssertionRawResponse = await makeWebauthnAssertion(
    options.fidoAssertionOptions
  )
  // Sign DWT with client session private key, to prove possession of private key
  dwt.cspkSig = await ed25519Sign(dwt.hash, csskB64)

  const makeAssertionRequest: MakeAssertionRequestModel = {
    clientResponse: assertionOptions,
    username,
    dwt
  }

  const response = await relyingPartyClient.fidoMakeAssertion(makeAssertionRequest)
  const authResponse: AssertionResponseModel = response.data
  const verifyAssertionResult: VerifyAssertionResult = authResponse.verifyAssertionResult
  const dwtBearerToken: string = authResponse.dwtBearerToken

  if (isNullOrUndefined(verifyAssertionResult?.credentialId)) {
    throw new Error('Credential ID undefined in RP auth response.')
  }

  // console.log('assertionOptions', assertionOptions)
  // webauthnLSIGClient.getGetWebauthnLSIG('test')

  return {
    bearerToken: dwtBearerToken,
    sessionKeyPK: cspkB64,
    sessionKeySK: csskB64
  }
}

export const register = async (username: string): Promise<MakeCredentialResponse> => {
  const displayName = username
  const makeCredOptions = await relyingPartyClient.fidoMakeCredentialOptions({
    username,
    displayName,
    attType: 'none',
    authType: isMobile() ? 'platform' : 'platform',
    residentKey: 'required',
    userVerification: 'required'
  })
  // Create Webauthn credential
  const makeCredentialsRequest: MakeCredentialsRequestModel = await makeWebauthnRegistration(makeCredOptions.data)
  const registerResponse = await relyingPartyClient.fidoMakeCredential(makeCredentialsRequest)
  const register: MakeCredentialResponse = registerResponse.data

  return register
}

export const getUsernameIsAvailable = async (username: string): Promise<boolean> => {
  const isAvailableResponse = await relyingPartyClient.fidoUsernameIsAvailable(username)
  const isAvailable: boolean = isAvailableResponse.data
  return isAvailable
}
