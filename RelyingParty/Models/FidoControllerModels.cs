using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.AspNetCore.Mvc;

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
    }

    public class AssertionResponseModel : Fido2ResponseBase
    {
        public VerifyAssertionResult VerifyAssertionResult { get; set; }
        public string dwtBearerToken { get; set; }
    }

    public class MakeAssertionRequestModel : Fido2ResponseBase
    {
        public AuthenticatorAssertionRawResponse clientResponse { get; set; }
        public string username { get; set; }
        public DWT dwt { get; set; }
    }

    public class MakeCredentialsRequestModel : Fido2ResponseBase
    {
        public string username { get; set; }
        public AuthenticatorAttestationRawResponse attestationResponse { get; set; }
    }
}