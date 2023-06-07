using AlgoStudio.Core.Attributes;
using AlgoStudio.Core;

namespace RelyingParty.Algorand.Signatures
{
    public class BasicSignature : SmartSignature
    {
        public override int Program()
        {
            InvokeSmartSignatureMethod();
            return 0; //fail if no smart signature method found
        }

        [SmartSignatureMethod("AssetTransfer")]
        public int ApproveAssetTransfer(
            AssetTransferTransactionReference txn,
            AssetReference masterKeyAsset,
            byte[] pkSess, // (pk_sess (32), rv_start (8), rv_end (8), random bytes)[0]
            ulong rvStart, // (pk_sess (32), rv_start (8), rv_end (8), random bytes)[1]
            ulong rvEnd, // (pk_sess (32), rv_start (8), rv_end (8), random bytes)[2]
            byte[] randB64, // (pk_sess (32), rv_start (8), rv_end (8), random bytes)[3]
            byte[] signCredR, byte[] signCredS, // sign_cred((pk_sess (32), rv_start (8), rv_end (8), random bytes))
            byte[] signSess // sign_sess(txn.TxID)
        )
        {
            // default checks            
            //do not permit any kind of rekey
            if (txn.RekeyTo != ZeroAddress) return 0;
            //do not permit any kind of close out
            if (txn.AssetCloseTo != ZeroAddress) return 0;
            //do not allow anything else than a single asset transfer
            if (GroupSize != 1) return 0;
            //do not permit any kind of rekey
            if (txn.RekeyTo != ZeroAddress) return 0;
            // Reject if the lease is null
            if (txn.Lease == null) return 0;

            // overflow checks
            if (rvStart > 4294967295) return 0;
            if (rvEnd > 4294967295) return 0;

            // combine parameters into the didt, once each is cast to bytes[]
            string rvStartString = rvStart.ToString();
            byte[] rvStartBytes = rvStartString.ToByteArray();
            string rvEndString = rvEnd.ToString();
            byte[] rvEndBytes = rvEndString.ToByteArray();
            // didt is (pk_sess (32), rv_start (8), rv_end (8), random bytes)
            byte[] didt = pkSess.Concat(rvStartBytes);
            didt = didt.Concat(rvEndBytes);
            didt = didt.Concat(randB64);

            // check didt has not expired
            // i.e. txn.FirstValid and txn.LastValid should be within the range of rvStart and rvEnd (session lifetime)
            if (txn.FirstValid > rvStart) return 0;
            if (txn.FirstValid < rvEnd) return 0;
            if (txn.LastValid > rvStart) return 0;
            if (txn.LastValid < rvEnd) return 0;

            // hard-coded credential public key
            byte[] pkCredX = { 0xde, 0xad, 0xbe, 0xef }; // to be string replaced by the controller
            byte[] pkCredY = { 0xde, 0xad, 0xca, 0xfe }; // to be string replaced by the controller

            // check sign_cred (credential signed the didt as valid) (i.e. the didt is valid)
            bool verifyCredentialSignedDidt = Ecdsa_verify_secp256r1(didt, signCredR, signCredS, pkCredX, pkCredY);
            if (verifyCredentialSignedDidt == false) return 0;

            // check sign_sess (session key signed the txn as valid) (i.e. the txn is valid)
            bool verifySessionSignedTxn = Ed25519verify(txn.TxID, signSess, pkSess);
            if (verifySessionSignedTxn == false) return 0;


            // verify the asset transfer is an asset transfer
            string txTypeCheck = "axfer";
            if (txn.TxType != txTypeCheck.ToByteArray()) return 0;

            // Must be: asset transfer with default checks, lease, valid didt (not expired, credential signed, contains
            // session key), valid txn (session key signed the txn as valid).

            return 1;
        }

        [SmartSignatureMethod("Payment")]
        public int ApprovePayment(
            PaymentTransactionReference txn,
            AssetReference masterKeyAsset,
            byte[] pkSess, // (pk_sess (32), rv_start (8), rv_end (8), random bytes)[0]
            ulong rvStart, // (pk_sess (32), rv_start (8), rv_end (8), random bytes)[1]
            ulong rvEnd, // (pk_sess (32), rv_start (8), rv_end (8), random bytes)[2]
            byte[] randB64, // (pk_sess (32), rv_start (8), rv_end (8), random bytes)[3]
            byte[] signCredR, byte[] signCredS, // sign_cred((pk_sess (32), rv_start (8), rv_end (8), random bytes))
            byte[] signSess // sign_sess(txn.TxID)
        )
        {
            // default checks            
            //do not permit any kind of rekey
            if (txn.RekeyTo != ZeroAddress) return 0;
            //do not permit any kind of close out
            if (txn.CloseRemainderTo != ZeroAddress) return 0;
            //do not allow anything else than a single asset transfer
            if (GroupSize != 1) return 0;
            //do not permit any kind of rekey
            if (txn.RekeyTo != ZeroAddress) return 0;
            // Reject if the lease is null
            if (txn.Lease == null) return 0;

            // overflow checks
            if (rvStart > 4294967295) return 0;
            if (rvEnd > 4294967295) return 0;

            // combine parameters into the didt, once each is cast to bytes[]
            string rvStartString = rvStart.ToString();
            byte[] rvStartBytes = rvStartString.ToByteArray();
            string rvEndString = rvEnd.ToString();
            byte[] rvEndBytes = rvEndString.ToByteArray();
            // didt is (pk_sess (32), rv_start (8), rv_end (8), random bytes)
            byte[] didt = pkSess.Concat(rvStartBytes);
            didt = didt.Concat(rvEndBytes);
            didt = didt.Concat(randB64);

            // check didt has not expired
            // i.e. txn.FirstValid and txn.LastValid should be within the range of rvStart and rvEnd (session lifetime)
            if (txn.FirstValid > rvStart) return 0;
            if (txn.FirstValid < rvEnd) return 0;
            if (txn.LastValid > rvStart) return 0;
            if (txn.LastValid < rvEnd) return 0;

            // hard-coded credential public key
            byte[] pkCredX = { 0xde, 0xad, 0xbe, 0xef }; // to be string replaced by the controller
            byte[] pkCredY = { 0xde, 0xad, 0xca, 0xfe }; // to be string replaced by the controller

            // check sign_cred (credential signed the didt as valid) (i.e. the didt is valid)
            bool verifyCredentialSignedDidt = Ecdsa_verify_secp256r1(didt, signCredR, signCredS, pkCredX, pkCredY);
            if (verifyCredentialSignedDidt == false) return 0;

            // check sign_sess (session key signed the txn as valid) (i.e. the txn is valid)
            bool verifySessionSignedTxn = Ed25519verify(txn.TxID, signSess, pkSess);
            if (verifySessionSignedTxn == false) return 0;


            // verify the payment is a payment
            string txTypeCheck = "pay";
            if (txn.TxType != txTypeCheck.ToByteArray()) return 0;

            // Must be: pay with default checks, lease, valid didt (not expired, credential signed, contains
            // session key), valid txn (session key signed the txn as valid).

            return 1;
        }

        [SmartSignatureMethod("AppCall")]
        public int ApprovePayment(
            AppCallTransactionReference txn,
            AssetReference masterKeyAsset,
            byte[] pkSess, // (pk_sess (32), rv_start (8), rv_end (8), random bytes)[0]
            ulong rvStart, // (pk_sess (32), rv_start (8), rv_end (8), random bytes)[1]
            ulong rvEnd, // (pk_sess (32), rv_start (8), rv_end (8), random bytes)[2]
            byte[] randB64, // (pk_sess (32), rv_start (8), rv_end (8), random bytes)[3]
            byte[] signCredR, byte[] signCredS, // sign_cred((pk_sess (32), rv_start (8), rv_end (8), random bytes))
            byte[] signSess // sign_sess(txn.TxID)
        )
        {
            // default checks            
            //do not permit any kind of rekey
            if (txn.RekeyTo != ZeroAddress) return 0;
            //do not allow anything else than a single asset transfer
            if (GroupSize != 1) return 0;
            //do not permit any kind of rekey
            if (txn.RekeyTo != ZeroAddress) return 0;
            // Reject if the lease is null
            if (txn.Lease == null) return 0;

            // overflow checks
            if (rvStart > 4294967295) return 0;
            if (rvEnd > 4294967295) return 0;

            // combine parameters into the didt, once each is cast to bytes[]
            string rvStartString = rvStart.ToString();
            byte[] rvStartBytes = rvStartString.ToByteArray();
            string rvEndString = rvEnd.ToString();
            byte[] rvEndBytes = rvEndString.ToByteArray();
            // didt is (pk_sess (32), rv_start (8), rv_end (8), random bytes)
            byte[] didt = pkSess.Concat(rvStartBytes);
            didt = didt.Concat(rvEndBytes);
            didt = didt.Concat(randB64);

            // check didt has not expired
            // i.e. txn.FirstValid and txn.LastValid should be within the range of rvStart and rvEnd (session lifetime)
            if (txn.FirstValid > rvStart) return 0;
            if (txn.FirstValid < rvEnd) return 0;
            if (txn.LastValid > rvStart) return 0;
            if (txn.LastValid < rvEnd) return 0;

            // hard-coded credential public key
            byte[] pkCredX = { 0xde, 0xad, 0xbe, 0xef }; // to be string replaced by the controller
            byte[] pkCredY = { 0xde, 0xad, 0xca, 0xfe }; // to be string replaced by the controller

            // check sign_cred (credential signed the didt as valid) (i.e. the didt is valid)
            bool verifyCredentialSignedDidt = Ecdsa_verify_secp256r1(didt, signCredR, signCredS, pkCredX, pkCredY);
            if (verifyCredentialSignedDidt == false) return 0;

            // check sign_sess (session key signed the txn as valid) (i.e. the txn is valid)
            bool verifySessionSignedTxn = Ed25519verify(txn.TxID, signSess, pkSess);
            if (verifySessionSignedTxn == false) return 0;


            // verify the payment is a payment
            string txTypeCheck = "appl";
            if (txn.TxType != txTypeCheck.ToByteArray()) return 0;

            // Must be: app call with default checks, lease, valid didt (not expired, credential signed, contains
            // session key), valid txn (session key signed the txn as valid).

            return 1;
        }
    }
}