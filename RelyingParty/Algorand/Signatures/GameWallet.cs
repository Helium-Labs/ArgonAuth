using AlgoStudio.Core.Attributes;
using AlgoStudio.Core;
using Algorand.Algod.Model.Transactions;
using Org.BouncyCastle.Crypto.Paddings;
using System.Text.RegularExpressions;
using Algorand.Algod.Model;

namespace RelyingParty.Algorand.Signatures
{
    public class BasicSignature : SmartSignature
    {
        AssetReference masterKeyAsset2;
        public override int Program()
        {
            InvokeSmartSignatureMethod();
            return 0; //fail if no smart signature method found
        }

        [SmartSignatureMethod("AssetTransfer")]
        /**
         * Args
         * [..., challenge, sign_pk_a(challenge), sign_pk_m(challenge), sign_pk_s(groupId, firstValid, lastValid)]
         * Challenge is (pk_s, expirationRound, rand_b64) 
         * groupIdFVLVSha256 is (groupId, firstValid, lastValid)
         */
        public int ApproveAssetTransfer(
            AssetTransferTransactionReference txn,
            AssetReference masterKeyAsset,
            byte[] pk_s_X, byte[] pk_s_Y, ulong expirationRound, byte[] rand_512bit, // Session details Challenge(pk_s, expirationRound, rand_b64)
            byte[] groupIdFVLVSha256, // sha256 hash of Tx details (groupId, firstValid, lastValid)
            byte[] sign_pk_a_R, byte[] sign_pk_a_S, // sign_pk_a(hash(challenge))
            byte[] sign_pk_m_R, byte[] sign_pk_m_S, // sign_pk_m(challenge)
            byte[] sign_pk_s_R, byte[] sign_pk_s_S // sign_pk_s(hash(groupId, firstValid, lastValid))
        )
        {
            /*
            // Challenge may be sent by a phishing site, so we need to verify that the challenge is correct
            byte[] pk_a_X = { 0xde, 0xad, 0xbe, 0xef }; // to be string replaced by the controller
            byte[] pk_a_Y = { 0xde, 0xad, 0xca, 0xfe }; // to be string replaced by the controller
            // ulong masterKeyAsset = 123456789; // to be string replaced by the controller
            byte[] masterKeyReserve = masterKeyAsset.Reserve;

            //usual checks
            //do not allow anything else than a single asset transfer
            if (GroupSize != 1) return 0;

            //do not permit any kind of rekey
            if (txn.RekeyTo != ZeroAddress) return 0;

            //do not permit any kind of close out
            if (txn.AssetCloseTo != ZeroAddress) return 0;

            // overflow checks
            if (expirationRound > 4294967295) return 0;
            // underflow checks
            if (expirationRound < 0) return 0;

            // generate challenge sha256
            byte[] expirationRoundBytes = expirationRound.ToTealBytes();
            byte[] challenge = pk_s_X.Concat(pk_s_Y);
            challenge = challenge.Concat(expirationRoundBytes);
            challenge = challenge.Concat(rand_512bit);
            // sha256 digest
            byte[] challengeSha256 = Sha256(challenge);

            // verify sign_pk_a(challenge)
            bool verifyAuthenticatorSignedChallenge = Ecdsa_verify_secp256r1(challengeSha256, sign_pk_a_R, sign_pk_a_S, pk_a_X, pk_a_Y);
            if (verifyAuthenticatorSignedChallenge == true) return 1;

            bool verifyMasterSignedChallenge = Ecdsa_verify_secp256r1(challengeSha256, sign_pk_m_R, sign_pk_m_S, pk_s_X, pk_s_Y);

            //make sure only asset transfers are permitted (because the tooling does not yet automatically verify marshalled txns, not sure if it should)
            string txTypeCheck = "axfer";
            if (txn.TxType != txTypeCheck.ToByteArray()) return 0;

            //reject if Lease is not set
            //TODO

            //Check that the concatenation of groupid, startround as bytes, endround as bytes, is signed by the address
            txn.FirstValid

            byte[] groupId = GroupId;
            byte[] message = groupId.Concat(startround);
            message = groupId.Concat(endround);

            bool check = Ecdsa_verify_secp256r1(message, signatureR, signatureS, pubkeyX, pubkeyY);

            if (check == true) return 1;
            else return 0;
            */
            return 0;
        }

        [SmartSignatureMethod("Payment")]
        public int ApprovePayment(PaymentTransactionReference txn)
        {
            //TODO - Only allow the master account to do a transfer
            // Make it more involved 
            return 0;
        }
    }
}
