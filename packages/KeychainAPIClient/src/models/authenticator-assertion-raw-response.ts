/* tslint:disable */
/* eslint-disable */
/**
 * KeychainRelyingParty API with Algorand SmartSig delegated access
 * A client for interfacing with the KeychainRelyingParty API
 *
 * OpenAPI spec version: v1
 * 
 *
 * NOTE: This class is auto generated by the swagger code generator program.
 * https://github.com/swagger-api/swagger-codegen.git
 * Do not edit the class manually.
 */
import { AssertionResponse } from './assertion-response';
import { AuthenticationExtensionsClientOutputs } from './authentication-extensions-client-outputs';
import { PublicKeyCredentialType } from './public-key-credential-type';
/**
 * 
 * @export
 * @interface AuthenticatorAssertionRawResponse
 */
export interface AuthenticatorAssertionRawResponse {
    /**
     * 
     * @type {string}
     * @memberof AuthenticatorAssertionRawResponse
     */
    id?: string | null;
    /**
     * 
     * @type {string}
     * @memberof AuthenticatorAssertionRawResponse
     */
    rawId?: string | null;
    /**
     * 
     * @type {AssertionResponse}
     * @memberof AuthenticatorAssertionRawResponse
     */
    response?: AssertionResponse | null;
    /**
     * 
     * @type {PublicKeyCredentialType}
     * @memberof AuthenticatorAssertionRawResponse
     */
    type?: PublicKeyCredentialType | null;
    /**
     * 
     * @type {AuthenticationExtensionsClientOutputs}
     * @memberof AuthenticatorAssertionRawResponse
     */
    extensions?: AuthenticationExtensionsClientOutputs | null;
}