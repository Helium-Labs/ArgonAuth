using Microsoft.AspNetCore.Mvc;

namespace RelyingParty.Models
{
    public class AssertionOptionsPostModel
    {
        public string Username { get; set; }
        public string UserVerification { get;set; }
    }
}
