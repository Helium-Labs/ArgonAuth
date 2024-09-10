import {
  type AuthenticatorAssertionRawResponse, type AssertionOptions,
  type PublicKeyCredentialType as PublicKeyCredentialTypeKeychain,
  type AuthenticatorAttestationRawResponse,
  type MakeCredentialsRequestModel,
  type CredentialCreateOptions,
  type DWT
} from '@gradian/keychain-auth-server-client'
import { AssertDefined, isNullOrUndefined } from './util'

import { type RegistrationResponseJSON, type AuthenticationResponseJSON, type PublicKeyCredentialDescriptorJSON, type PublicKeyCredentialCreationOptionsJSON } from '@simplewebauthn/types'
// @ts-ignore
import { startAuthentication, startRegistration } from '@simplewebauthn/browser'

export async function makeWebauthnAssertion(options: AssertionOptions): Promise<AuthenticatorAssertionRawResponse> {
  const fidoAssertionOptions: AssertionOptions = toLower(options)
  const allowCredentials: PublicKeyCredentialDescriptorJSON[] = fidoAssertionOptions.allowCredentials!
    .filter((cred) => {
      return !isNullOrUndefined(cred.type) && !isNullOrUndefined(cred.id)
    })
    .map((cred) => {
      AssertDefined(cred.id, 'cred.id must be defined')
      AssertDefined(cred.transports, 'cred.transports must be defined')
      const result: PublicKeyCredentialDescriptorJSON = {
        id: cred.id,
        type: 'public-key',
        transports: cred.transports
      }
      return result
    })
  const startAuthOptions = {
    challenge: fidoAssertionOptions.challenge,
    timeout: fidoAssertionOptions.timeout,
    rpId: fidoAssertionOptions.rpId,
    allowCredentials,
    userVerification: fidoAssertionOptions.userVerification,
    extensions: fidoAssertionOptions.extensions
  }
  //@ts-ignore
  const assertionResponse: AuthenticationResponseJSON = await startAuthentication(startAuthOptions)
  const assertionOptions: AuthenticatorAssertionRawResponse = {
    ...assertionResponse,
    type: 'PublicKey' as PublicKeyCredentialTypeKeychain
  }

  return assertionOptions
}

export async function makeWebauthnRegistration(
  createOptions: CredentialCreateOptions,
  dwt: DWT,
  redirectUri: string,
  state: string,
  codeChallenge: string,
  emailCode: string
): Promise<MakeCredentialsRequestModel> {
  const options: CredentialCreateOptions = toLower(createOptions)
  AssertDefined(options.pubKeyCredParams, 'options.pubKeyCredParams must be defined')
  const pubKeyCredParams = options.pubKeyCredParams.map((param) => {
    AssertDefined(param.alg, 'param.alg must be defined')
    const pubkeyCredParam: PublicKeyCredentialParameters = {
      type: 'public-key',
      alg: coseAlgorithmToIdentifier(param.alg)
    }
    return pubkeyCredParam
  })

  AssertDefined(options.excludeCredentials, 'options.excludeCredentials must be defined')
  const excludeCredentials = options.excludeCredentials.map((cred) => {
    AssertDefined(cred.id, 'cred.id must be defined')
    AssertDefined(cred.transports, 'cred.transports must be defined')
    const excludeCredential: PublicKeyCredentialDescriptorJSON = {
      id: cred.id,
      type: 'public-key',
      transports: cred.transports
    }
    return excludeCredential
  })

  AssertDefined(options.user, "User must be defined")
  const registrationOptions: PublicKeyCredentialCreationOptionsJSON = {
    rp: { id: undefined, name: options!.rp!.name! },
    user: {
      id: options.user.id!,
      name: options.user.name!,
      displayName: options.user.displayName!
    },
    challenge: options.challenge!,
    pubKeyCredParams,
    timeout: options.timeout,
    excludeCredentials,
    authenticatorSelection: options.authenticatorSelection! as AuthenticatorSelectionCriteria,
    attestation: options.attestation,
    extensions: options.extensions as AuthenticationExtensionsClientInputs
  }

  const registrationResponse: RegistrationResponseJSON =
    await startRegistration(registrationOptions)
  const registration: AuthenticatorAttestationRawResponse =
    registrationResponse as AuthenticatorAttestationRawResponse
  const makeCredentialsRequest: MakeCredentialsRequestModel = {
    attestationResponse: {
      ...registration,
      extensions: registrationResponse.clientExtensionResults,
      type: 'PublicKey' as PublicKeyCredentialTypeKeychain
    },
    username: createOptions.user!.displayName!,
    emailCode,
    state,
    codeChallenge,
    dwt,
    redirectUri
  }

  return makeCredentialsRequest
}

function coseAlgorithmToIdentifier(algorithm: string): COSEAlgorithmIdentifier {
  if (algorithm == null) {
    throw new Error('COSE Algorithm is required')
  }

  const mapping: Record<string, number> = {
    RS1: -65535,
    RS512: -259,
    RS384: -258,
    RS256: -257,
    ES256K: -47,
    PS512: -39,
    PS384: -38,
    PS256: -37,
    ES512: -36,
    ES384: -35,
    EdDSA: -8,
    ES256: -7
  }
  AssertDefined(mapping[algorithm], "mapping[algorithm] must be defined")
  return mapping[algorithm]!
}

function toLower(obj: any): any {
  const toLowerFields = ['authenticatorAttachment', 'userVerification', 'attestation', 'residentKey']
  // convert CamelCase to dash-case, e.g. CrossPlatform -> cross-platform
  const convertToDashCaseIfCamelCase = (str: string): string => {
    return str.replace(/([a-z])([A-Z])/g, '$1-$2').toLowerCase()
  }

  if (typeof obj !== 'object') {
    return obj
  }

  for (const key in obj) {
    if (toLowerFields.includes(key)) {
      obj[key] = convertToDashCaseIfCamelCase(obj[key]).toLowerCase()
    }
    obj[key] = toLower(obj[key])
  }

  return obj
}
