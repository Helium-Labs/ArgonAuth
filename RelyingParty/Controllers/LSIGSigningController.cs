using Fido2NetLib;
using Microsoft.AspNetCore.Mvc;
using RelyingParty.Algorand.ServerAccount;
using AlgoStudio.Clients;
using Fido2NetLib.Cbor;
using Algorand.Algod;
using RelyingParty.Algorand.Signatures;
using Algorand.KMD;
using Algorand;
using Algorand.Algod.Model.Transactions;
using Algorand.Utils;
using LSIGSign.Models;
using RelyingParty.Utilities;
using Proxies;
using RelyingParty.Data;
using RelyingParty.Models;

namespace LSIGSign.Handlers;

[Route("api/[controller]")]
public class LSIGSigningController : Controller
{
    private IDefaultApi _algodApi;
    public static IMetadataService _mds;
    public static PlanetScaleDatabase _db;


    private readonly ILogger _logger;

    public LSIGSigningController(ILogger<LSIGSigningController> logger, IMasterAccount serverAccount,
        IDefaultApi algod, IApi kmdApi,
        PlanetScaleDatabase database)
    {
        _algodApi = algod;
        _db = database;
        _logger = logger;
    }

    private string FormatException(Exception e)
    {
        return string.Format("{0}{1}", e.Message,
            e.InnerException != null ? " (" + e.InnerException.Message + ")" : "");
    }

    [HttpPost]
    [Route("/LSIGSignTransaction")]
    public async Task<string> LSIGSignTransaction([FromBody] LSIGSignOptionsModel lsigOptions)
    {
        try
        {
            Transaction transaction = lsigOptions.Transaction;
            // decode the base64 encoded credential id into a byte array
            string base64EncodedCredentialID = lsigOptions.Base64EncodedCredentialID;
            byte[] credentialID = Convert.FromBase64String(base64EncodedCredentialID);

            // decode the base64 encoded signature into a byte array
            string base64EncodedTxSessSignature = lsigOptions.Base64EncodedTxSessSignature;
            byte[] txSessSignature = Convert.FromBase64String(base64EncodedTxSessSignature);

            SignedDidt signedDidt = await _db.GetSignedDidt(credentialID) ?? throw new Exception("User not found");
            byte[] didt = signedDidt.didt;
            byte[] didtSignature = signedDidt.signature;
            byte[] didtSignatureR = UtilityMethods.GetRValueFromSignature(didtSignature);
            byte[] didtSignatureS = UtilityMethods.GetSValueFromSignature(didtSignature);

            byte[] didtPubKey = UtilityMethods.GetSessionPublicKeyFromDidt(didt);
            ulong didtRoundStart = UtilityMethods.GetRVStart(didt);
            ulong didtRoundEnd = UtilityMethods.GetRVEnd(didt);
            byte[] didtRandomBytes = UtilityMethods.GetRandomBytesFromDidt(didt);

            // get stored credential by id
            StoredCredential storedCredential = await _db.GetCredentialById(credentialID) ?? throw new Exception("Credential not found");
            byte[] credPublicKey = storedCredential.PublicKey;
            var decodedPubKey = (CborMap)CborObject.Decode(credPublicKey);
            // X and Y values represent the coordinates of a point on the elliptic curve, constituting the public key
            byte[] pubkeyX = (byte[])decodedPubKey.GetValue(-2);
            byte[] pubkeyY = (byte[])decodedPubKey.GetValue(-3);
            AccountGameWallet lsig = new AccountGameWallet(pubkeyX, pubkeyY);
            LogicsigSignature lsigCompiled = await lsig.Compile((DefaultApi)_algodApi);
            GameWalletSigner lsigSigner = new GameWalletSigner(lsigCompiled);

            switch (transaction)
            {
                case PaymentTransaction paymentTransaction:
                    lsigSigner.ApprovePayment(didtPubKey, didtRoundStart, didtRoundEnd, didtRandomBytes, didtSignatureR, didtSignatureS, txSessSignature);
                    break;
                case AssetTransferTransaction assetTransferTransaction:
                    lsigSigner.ApproveAssetTransfer(didtPubKey, didtRoundStart, didtRoundEnd, didtRandomBytes, didtSignatureR, didtSignatureS, txSessSignature);
                    break;
                case ApplicationCallTransaction applicationCallTransaction:
                    lsigSigner.ApproveAppCall(didtPubKey, didtRoundStart, didtRoundEnd, didtRandomBytes, didtSignatureR, didtSignatureS, txSessSignature);
                    break;
                default:
                    throw new Exception("Transaction type not supported");
            }

            // sign the transaction with the smartsig
            SignedTransaction signedTransaction = transaction.Sign(lsigCompiled);

            var txRes = await _algodApi.TransactionsAsync(new List<SignedTransaction> { signedTransaction });
            var resp = await Utils.WaitTransactionToComplete((DefaultApi)_algodApi, txRes.Txid) as Transaction;

            // var transParams = await _algodApi.TransactionParamsAsync();
            return resp.TxID();
        }
        catch (Exception e)
        {
            // print out the exception nicely to console with writeln, respond with 500 error to caller with exception message
            Console.WriteLine(FormatException(e));
            // Must explicitly set status code, otherwise it's a 200
            Response.StatusCode = 500;
            return FormatException(e);
        }
    }
}