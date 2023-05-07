using Fido2NetLib;

namespace RelyingParty.AlgorandFidoExtensions
{
    public class AssertionOptionsResponse
    {
        public AssertionOptions FidoAssertionOptions { get; set; }
        public ulong CurrentRound { get; set; }
    }
}
