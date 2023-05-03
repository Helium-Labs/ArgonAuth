using AlgoStudio.Core.Attributes;
using AlgoStudio.Core;
using Algorand.Algod.Model.Transactions;
using Org.BouncyCastle.Crypto.Paddings;

namespace RelyingParty.Algorand.Signatures
{
    public class BasicSignature : SmartSignature
    {
        public override int Program()
        {
            InvokeSmartSignatureMethod();
            return 0; //fail if no smart signature method found
        }

        [SmartSignatureMethod("Ax1")]
        public bool ApproveTransferClient(AssetTransferTransactionReference txn, byte[] signatureR, byte[] signatureS, byte[] startround, byte[] endround)
        {
            //do not allow anything else than a single asset transfer
            if (GroupSize != 1) return 0;

            //do not permit any kind of rekey
            if (txn.RekeyTo != ZeroAddress) return 0;

            //do not permit any kind of close out
            if (txn.AssetCloseTo != ZeroAddress) return 0;

            //make sure only asset transfers are permitted (because the tooling does not yet automatically verify marshalled txns, not sure if it should)
            string txTypeCheck = "axfer";
            if (txn.TxType != txTypeCheck.ToByteArray()) return 0;

            //reject if Lease is not set
            //TODO

            //Check that the concatenation of groupid, startround as bytes, endround as bytes, is signed by the address
            byte[] groupId = GroupId;
            byte[] message = groupId.Concat(startround);
            message = groupId.Concat(endround);

            byte[] pubkeyX = { 0xde, 0xad, 0xbe, 0xef }; // to be string replaced by the controller
            byte[] pubkeyY = { 0xde, 0xad, 0xca, 0xfe }; // to be string replaced by the controller

            bool check=Ecdsa_verify_secp256r1(message, signatureR, signatureS, pubkeyX, pubkeyY);


            return check;
        }

        [SmartSignatureMethod("Ax1Delegated")]
        public bool ApproveTransferDelegated(AssetTransferTransactionReference txn, byte[] signatureR, byte[] signatureS, byte[] proofKey,  ulong startround, ulong endround)
        {
            //do not allow anything else than a single asset transfer
            if (GroupSize != 1) return 0;

            //do not permit any kind of rekey
            if (txn.RekeyTo != ZeroAddress) return 0;

            //do not permit any kind of close out
            if (txn.AssetCloseTo != ZeroAddress) return 0;

            //make sure only asset transfers are permitted (because the tooling does not yet automatically verify marshalled txns, not sure if it should)
            string txTypeCheck = "axfer";
            if (txn.TxType != txTypeCheck.ToByteArray()) return 0;

            //reject if Lease is not set

            //Hash the proofKey and check that the signature of concat(hash+startround+endround) is signed by the address.
            
            



            return 1;
        }

        [SmartSignatureMethod("Payment")]
        public int ApprovePayment(PaymentTransactionReference txn)
        {
            //TODO - Only allow the master account to do a transfer
            
            return 0;
        }
    }
}
