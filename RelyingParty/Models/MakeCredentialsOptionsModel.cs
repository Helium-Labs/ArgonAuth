namespace RelyingParty.Models
{
    public class MakeCredentialOptionsModel
    {
        public string Username { get; set; }
        public string DisplayName { get; set; }
        public string AttType { get; set; }
        public string AuthType { get; set; }
        public bool ResidentKey { get; set; }
        public string UserVerification { get; set; }
    }
}
