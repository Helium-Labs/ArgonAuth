using Algorand.Algod.Model;

namespace RelyingParty.Algorand.ServerAccount
{
    public class MasterAccount : IMasterAccount
    {
        private Account masterAccount;

        private MasterAccount() { }

        public MasterAccount(IConfiguration config)
        {
            string accountKey=config["serverAccount:mnemonic"];
            masterAccount=new Account(accountKey);
        }

        public Account Account => masterAccount;

    }
}
