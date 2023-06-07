using AlgoStudio;
using Algorand;
using System.Text;

namespace Proxies
{
    public class GameWalletSigner : SignatureBase
    {
        LogicsigSignature smartSig;

        public GameWalletSigner(LogicsigSignature logicSig) : base(logicSig)
        {
        }

        public void ApproveAssetTransfer(byte[] pkSess, ulong rvStart, ulong rvEnd, byte[] randB64, byte[] signCredR, byte[] signCredS, byte[] signSess)
        {
            var abiHandle = Encoding.UTF8.GetBytes("AssetTransfer");
            base.UpdateSmartSignature(new List<object> { abiHandle, pkSess, rvStart, rvEnd, randB64, signCredR, signCredS, signSess });
        }

        public void ApprovePayment(byte[] pkSess, ulong rvStart, ulong rvEnd, byte[] randB64, byte[] signCredR, byte[] signCredS, byte[] signSess)
        {
            var abiHandle = Encoding.UTF8.GetBytes("Payment");
            base.UpdateSmartSignature(new List<object> { abiHandle, pkSess, rvStart, rvEnd, randB64, signCredR, signCredS, signSess });
        }

        public void ApproveAppCall(byte[] pkSess, ulong rvStart, ulong rvEnd, byte[] randB64, byte[] signCredR, byte[] signCredS, byte[] signSess)
        {
            var abiHandle = Encoding.UTF8.GetBytes("AppCall");
            base.UpdateSmartSignature(new List<object> { abiHandle, pkSess, rvStart, rvEnd, randB64, signCredR, signCredS, signSess });
        }
    }
}
