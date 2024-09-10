using Fido2NetLib;
using RelyingParty.Models;

namespace RelyingParty.AlgorandFidoExtensions
{
    public class AssertionOptionsResponse
    {
        public AssertionOptions FidoAssertionOptions { get; set; }
        public DWT? DWT { get; set; }
    }
}
