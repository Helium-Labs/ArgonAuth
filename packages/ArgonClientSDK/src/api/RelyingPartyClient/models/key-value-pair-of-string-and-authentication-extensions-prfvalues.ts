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
import { AuthenticationExtensionsPRFValues } from './authentication-extensions-prfvalues';
/**
 * 
 * @export
 * @interface KeyValuePairOfStringAndAuthenticationExtensionsPRFValues
 */
export interface KeyValuePairOfStringAndAuthenticationExtensionsPRFValues {
    /**
     * 
     * @type {string}
     * @memberof KeyValuePairOfStringAndAuthenticationExtensionsPRFValues
     */
    key?: string;
    /**
     * 
     * @type {AuthenticationExtensionsPRFValues}
     * @memberof KeyValuePairOfStringAndAuthenticationExtensionsPRFValues
     */
    value?: AuthenticationExtensionsPRFValues;
}
