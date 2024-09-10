using static Fido2NetLib.Fido2;

namespace RelyingParty.AlgorandFidoExtensions
{
    /// <summary>
    /// The Algorand FIDO2 Extension 
    /// </summary>
    public class MakeCredentialResponse
    {
        public CredentialMakeResult FidoCredentialMakeResult { get; set; }
        public string Code { get; set; }
    }
}
