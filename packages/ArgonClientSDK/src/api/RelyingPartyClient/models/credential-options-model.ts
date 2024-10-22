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
import { CredentialCreateOptions } from './credential-create-options';
import { DWT } from './dwt';
import { Fido2ResponseBase } from './fido2-response-base';
/**
 * 
 * @export
 * @interface CredentialOptionsModel
 */
export interface CredentialOptionsModel extends Fido2ResponseBase {
    /**
     * 
     * @type {CredentialCreateOptions}
     * @memberof CredentialOptionsModel
     */
    options?: CredentialCreateOptions;
    /**
     * 
     * @type {DWT}
     * @memberof CredentialOptionsModel
     */
    dwt?: DWT;
}
