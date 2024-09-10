using Fido2NetLib;
using Fido2NetLib.Objects;
using System.Text.Json.Serialization;

namespace RelyingParty.Models
{
    public class AssertOptionsRequestModel
    {
        public string Username { get; set; }
        public string UserVerification { get; set; }
    }

    public class MakeCredentialOptionsModel
    {
        public string Username { get; set; }
        public string DisplayName { get; set; }
        public string AttType { get; set; }
        public string AuthType { get; set; }
        public string ResidentKey { get; set; }
        public string UserVerification { get; set; }

        /// <summary>
        /// This extension allows a Relying Party to evaluate outputs from a pseudo-random function (PRF) associated with a credential.
        /// https://w3c.github.io/webauthn/#prf-extension
        /// </summary>
        [JsonPropertyName("prf")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public AuthenticationExtensionsPRFInputs? PRF { get; set; }
    }

    public class CredentialOptionsModel : Fido2ResponseBase
    {
        public CredentialCreateOptions Options { get; set; }
        public DWT DWT { get; set; }
    }
    public class AssertionResponseModel : Fido2ResponseBase
    {
        public VerifyAssertionResult VerifyAssertionResult { get; set; }
        public string? Code { get; set; }
    }

    public class MakeAssertionRequestModel : Fido2ResponseBase
    {
        public AuthenticatorAssertionRawResponse clientResponse { get; set; }
        public string username { get; set; }
        public DWT dwt { get; set; }
        
        public string state { get; set; }
        public string codeChallenge { get; set; }
        
        public string redirectUri { get; set; }
    }

    public class MakeCredentialsRequestModel : Fido2ResponseBase
    {
        public string username { get; set; }
        public string emailCode { get; set; }
        
        public DWT dwt { get; set; }
        
        public string state { get; set; }
        public string codeChallenge { get; set; }
        
        public string redirectUri { get; set; }
        public AuthenticatorAttestationRawResponse attestationResponse { get; set; }
    }
    
    public class GetExchangeCodeRequestModel : Fido2ResponseBase
    {
        // Specify the code
        public string code { get; set; }
        // Specify the state
        public string state { get; set; }
        // Specify the pkce code verifier
        public string codeVerifier { get; set; }
        // Redirect URI
        public string redirectUri { get; set; }
        // Specify the client id
        public string clientId { get; set; }
        // Specify the client secret
        public string clientSecret { get; set; }
    }
    
    // Model to represent the request body for verifying the email code
    public class VerifyEmailCodeRequestModel
    {
        public string Email { get; set; }
        public string Code { get; set; }
    }
}