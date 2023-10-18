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
}