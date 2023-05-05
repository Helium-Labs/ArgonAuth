using AlgoStudio.Core;

namespace RelyingParty.Algorand.Signatures
{
    public class AccountGameWallet : ICompiledSignature
    {
        private string program;

        public AccountGameWallet(byte[] pubKeyX,byte[] pubkeyY)
        {
            var lsig = new GameWallet.BasicSignature();

            //TODO need to dream up a better way I guess
            program = lsig.Program.Replace("0xDEADBEEF", Convert.ToHexString(pubKeyX));
            program = program.Replace("0xDEADCAFE", Convert.ToHexString(pubkeyY));


        }

        public string Program => program;
    }
}
