/* tslint:disable */
/* eslint-disable */
/**
 * ArgonRelyingParty API with Algorand SmartSig delegated access
 * A client for interfacing with the ArgonRelyingParty API
 *
 * OpenAPI spec version: v1
 * 
 *
 * NOTE: This class is auto generated by the swagger code generator program.
 * https://github.com/swagger-api/swagger-codegen.git
 * Do not edit the class manually.
 */
import { AuthenticationExtensionsClientOutputs } from './authentication-extensions-client-outputs';
import { PublicKeyCredentialType } from './public-key-credential-type';
import { ResponseData } from './response-data';
/**
 * 
 * @export
 * @interface AuthenticatorAttestationRawResponse
 */
export interface AuthenticatorAttestationRawResponse {
    /**
     * 
     * @type {string}
     * @memberof AuthenticatorAttestationRawResponse
     */
    id?: string | null;
    /**
     * 
     * @type {string}
     * @memberof AuthenticatorAttestationRawResponse
     */
    rawId?: string | null;
    /**
     * 
     * @type {PublicKeyCredentialType}
     * @memberof AuthenticatorAttestationRawResponse
     */
    type?: PublicKeyCredentialType;
    /**
     * 
     * @type {ResponseData}
     * @memberof AuthenticatorAttestationRawResponse
     */
    response?: ResponseData | null;
    /**
     * 
     * @type {AuthenticationExtensionsClientOutputs}
     * @memberof AuthenticatorAttestationRawResponse
     */
    extensions?: AuthenticationExtensionsClientOutputs | null;
}