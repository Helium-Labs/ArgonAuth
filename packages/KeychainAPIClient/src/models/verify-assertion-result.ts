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
import { Fido2ResponseBase } from './fido2-response-base';
/**
 * 
 * @export
 * @interface VerifyAssertionResult
 */
export interface VerifyAssertionResult extends Fido2ResponseBase {
    /**
     * 
     * @type {string}
     * @memberof VerifyAssertionResult
     */
    credentialId?: string | null;
    /**
     * 
     * @type {number}
     * @memberof VerifyAssertionResult
     */
    signCount?: number;
    /**
     * 
     * @type {boolean}
     * @memberof VerifyAssertionResult
     */
    isBackedUp?: boolean;
    /**
     * 
     * @type {string}
     * @memberof VerifyAssertionResult
     */
    devicePublicKey?: string | null;
}