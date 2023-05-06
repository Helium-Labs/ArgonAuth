using Fido2NetLib;
using Fido2NetLib.Development;
using Fido2NetLib.Objects;
using Microsoft.AspNetCore.Mvc;
using RelyingParty.Algorand.ServerAccount;
using RelyingParty.AlgorandFidoExtensions;
using RelyingParty.Models;
using System.Text;
using AlgoStudio.Clients;
using static Fido2NetLib.Fido2;
using System.Configuration;
using Fido2NetLib.Cbor;
using Algorand.Algod;
using RelyingParty.Algorand.Signatures;
using Algorand.KMD;
using Algorand.Algod.Model;
using Algorand;
using Algorand.Algod.Model.Transactions;
using Algorand.Utils;

namespace Fido2Demo;

[Route("api/[controller]")]
public class Fido2Controller : Controller
{
    private IFido2 _fido2;
    private IMasterAccount _serverAccount;
    private IDefaultApi _algodApi;
    public static IMetadataService _mds;
    public static PlanetScaleDatabase _db;
    private static Api _kmdApi;
    private const string walletName = "unencrypted-default-wallet";
    //DEMO CODE
    private static Account account1; 
    private static Account account2;
    private static Account account3;


    public Fido2Controller(IFido2 fido2, IMasterAccount serverAccount, IDefaultApi algod, IApi kmdApi ,PlanetScaleDatabase database)
    {
        _algodApi = algod;
        _fido2 = fido2;
        _serverAccount = serverAccount;
        _db = database;
        _kmdApi = (Api)kmdApi;

       
    }

    private string FormatException(Exception e)
    {
        return string.Format("{0}{1}", e.Message, e.InnerException != null ? " (" + e.InnerException.Message + ")" : "");
    }

    [HttpPost]
    [Route("/makeCredentialOptions")]
    public CredentialCreateOptions MakeCredentialOptions(MakeCredentialOptionsModel model)
    {
        try
        {

            if (string.IsNullOrEmpty(model.Username))
            {
                model.Username = $"{model.DisplayName} (Usernameless user created at {DateTime.UtcNow})";
            }

            // 1. Get user from DB by username (in our example, auto create missing users)
            var user = _db.GetOrAddUser(model.Username, () => new Fido2User
            {
                DisplayName = model.DisplayName,
                Name = model.Username,
                Id = Encoding.UTF8.GetBytes(model.Username) // byte representation of userID is required
            });

            // 2. Get user existing keys by username
            var existingKeys = _db.GetCredentialsByUser(user).Select(c => c.Descriptor).ToList();

            // 3. Create options
            var authenticatorSelection = new AuthenticatorSelection
            {
                RequireResidentKey = model.ResidentKey,
                UserVerification = model.UserVerification.ToEnum<UserVerificationRequirement>()
            };

            if (!string.IsNullOrEmpty(model.AuthType))
                authenticatorSelection.AuthenticatorAttachment = model.AuthType.ToEnum<AuthenticatorAttachment>();

            var exts = new AuthenticationExtensionsClientInputs()
            {
                Extensions = true,
                UserVerificationMethod = true,
            };

            var options = _fido2.RequestNewCredential(user, existingKeys, authenticatorSelection, model.AttType.ToEnum<AttestationConveyancePreference>(), exts);
            options.PubKeyCredParams = options.PubKeyCredParams.Where(o => o.Alg == COSE.Algorithm.ES256).ToList();
            // 4. Temporarily store options, session/in-memory cache/redis/db
            HttpContext.Session.SetString("fido2.attestationOptions", options.ToJson());

            // 5. return options to client
            return options;
        }
        catch (Exception e)
        {
            return new CredentialCreateOptions { Status = "error", ErrorMessage = FormatException(e) };
        }
    }

    [HttpPost]
    [Route("/makeCredential")]
    public async Task<MakeCredentialResponse> MakeCredential([FromBody] AuthenticatorAttestationRawResponse attestationResponse, CancellationToken cancellationToken)
    {
        try
        {
            // 1. get the options we sent the client
            var jsonOptions = HttpContext.Session.GetString("fido2.attestationOptions");
            var options = CredentialCreateOptions.FromJson(jsonOptions);

            // 2. Create callback so that lib can verify credential id is unique to this user
            IsCredentialIdUniqueToUserAsyncDelegate callback = static async (args, cancellationToken) =>
            {
                var users = await _db.GetUsersByCredentialIdAsync(args.CredentialId, cancellationToken);
                if (users.Count > 0)
                    return false;

                return true;
            };

            // 2. Verify and make the credentials
            var success = await _fido2.MakeNewCredentialAsync(attestationResponse, options, callback, cancellationToken: cancellationToken);

            // 3. Store the credentials in db

            //TODO:

            _db.AddCredentialToUser(options.User, new StoredCredential
            {
                Descriptor = new PublicKeyCredentialDescriptor(success.Result.CredentialId),
                PublicKey = success.Result.PublicKey,
                UserHandle = success.Result.User.Id,
                SignatureCounter = success.Result.Counter,
                CredType = success.Result.CredType,
                RegDate = DateTime.UtcNow,
           //     AaGuid = success.Result.Aaguid
            });

            // Remove Certificates from success because System.Text.Json cannot serialize them properly. See https://github.com/passwordless-lib/fido2-net-lib/issues/328
            success.Result.AttestationCertificate = null;
            success.Result.AttestationCertificateChain = null;


            //get pubkey
            var decodedPubKey=(CborMap)CborObject.Decode(success.Result.PublicKey);
            byte[] pubkeyX = (byte[])decodedPubKey.GetValue(-2);
            byte[] pubkeyY = (byte[])decodedPubKey.GetValue(-3);

            //Modify our logic signature
            var lsig = new AccountGameWallet(pubkeyX, pubkeyY);

            //Compile it
            //TODO - compile should accept IDefaultApi and IDefaultApi should have a meaningful name that doesnt rely on its namespace
            var lsigCompiled = await lsig.Compile((DefaultApi)_algodApi);

            // DEMO CODE
            await SetUpAccounts();

            await preFundLsig(lsigCompiled);

            MakeCredentialResponse response = new MakeCredentialResponse()
            {
                FidoCredentialMakeResult = success,
                LogicSignatureProgram = lsigCompiled.Logic
            };

            return response;
        }
        catch (Exception e)
        {
            return new MakeCredentialResponse()
            {
                FidoCredentialMakeResult = new CredentialMakeResult(status: "error", errorMessage: FormatException(e), result: null),
                LogicSignatureProgram = null
            };
        }
    }

    /// <summary>
    /// Pre-fund the Lsig with whatever.....
    /// 
    /// TODO - Assuming this is going to be a centralised service for game developers, the particular master account, 
    /// the particular initial funding MBR and assets, would want to come from a config depending on the route.
    /// 
    /// 
    /// </summary>
    private async Task preFundLsig(LogicsigSignature lsigCompiled)
    {
        //DEMO CODE
        //  Just assume Sandbox here and chuck some funds at the lsig

        var transParams = await _algodApi.TransactionParamsAsync();
        var payment = PaymentTransaction.GetPaymentTransactionFromNetworkTransactionParameters(account1.Address, lsigCompiled.Address, 1000000, "", transParams);
        var signed= payment.Sign(account1);
        try
        {
            //send the transaction for processing
            var txRes = await ((DefaultApi)_algodApi).TransactionsAsync(new List<SignedTransaction> { signed });
            var resp = await Utils.WaitTransactionToComplete((DefaultApi)_algodApi, txRes.Txid) ;

        }
        catch (Algorand.ApiException<ErrorResponse> ex)
        {
            System.Diagnostics.Trace.WriteLine(ex.ToString());
        }

    }

    [HttpPost]
    [Route("/assertionOptions")]
    public async Task<AssertionOptionsResponse> AssertionOptionsPost( AssertionOptionsPostModel assertionOptions)
    {
        try
        {
            var existingCredentials = new List<PublicKeyCredentialDescriptor>();

            if (!string.IsNullOrEmpty(assertionOptions.Username))
            {
                // 1. Get user from DB
                var user = _db.GetUser(assertionOptions.Username) ?? throw new ArgumentException("Username was not registered");

                // 2. Get registered credentials from database
                existingCredentials = _db.GetCredentialsByUser(user).Select(c => c.Descriptor).ToList();
            }
            
            //TODO verify PK and change db schema
            foreach (var c in existingCredentials)
            {
                c.Id = c.Id.Take(64).ToArray();
            }


            var exts = new AuthenticationExtensionsClientInputs()
            {
                UserVerificationMethod = true
            };

            // 3. Create options
            var uv = string.IsNullOrEmpty(assertionOptions.UserVerification) ? UserVerificationRequirement.Discouraged : assertionOptions.UserVerification.ToEnum<UserVerificationRequirement>();
            var options = _fido2.GetAssertionOptions(
                existingCredentials,
                uv,
                exts
            );

            // DEMO: Replace the Challenge with a hashed Challenge
            var hashedChallenge=Digester.Digest(options.Challenge);
            HttpContext.Session.SetString("gradian.delegationSecret", options.ToJson()); 
            options.Challenge=hashedChallenge;

            // 4. Temporarily store options, session/in-memory cache/redis/db
            HttpContext.Session.SetString("fido2.assertionOptions", options.ToJson());

            var transParams = await _algodApi.TransactionParamsAsync();

            // 5. Return options to client
            return new AssertionOptionsResponse() { FidoAssertionOptions = options, CurrentRound = transParams.LastRound };
        }
        catch (Exception e)
        {
            return new AssertionOptionsResponse() { FidoAssertionOptions = new AssertionOptions { Status = "error", ErrorMessage = FormatException(e) } };
        }
    }
    private byte[] ulongToBigEndianBytes(ulong l)
    {
        IEnumerable<byte> res = BitConverter.GetBytes(l);
        if (BitConverter.IsLittleEndian)
        {
            res = res.Reverse();
        }
        return res.ToArray();
    }
    [HttpPost]
    [Route("/makeAssertionAndDelegateAccess")]
    public async Task<AssertionVerificationResult> MakeAssertionAndDelegateAccess( AuthenticatorAssertionRawResponse clientResponse, ulong roundStart, ulong roundEnd, CancellationToken cancellationToken)
    {
        try
        {
            // 1. Get the assertion options we sent the client
            var jsonOptions = HttpContext.Session.GetString("fido2.assertionOptions");
            var options = AssertionOptions.FromJson(jsonOptions);

            // 2. Get registered credential from database
            var creds = _db.GetCredentialById(clientResponse.Id) ?? throw new Exception("Unknown credentials");

            // 3. Get credential counter from database
            var storedCounter = creds.SignatureCounter;

            // 4. Create callback to check if userhandle owns the credentialId
            IsUserHandleOwnerOfCredentialIdAsync callback = static async (args, cancellationToken) =>
            {
                var storedCreds = await _db.GetCredentialsByUserHandleAsync(args.UserHandle, cancellationToken);
                return storedCreds.Exists(c => c.Descriptor.Id.SequenceEqual(args.CredentialId));
            };

            // 5. Make the assertion

            // DEMO : modify the challenge to contain the bigendian concatenation of roundstart and roundend
            options.Challenge = options.Challenge.Concat(ulongToBigEndianBytes(roundStart)).Concat(ulongToBigEndianBytes(roundEnd)).ToArray();

            var res = await _fido2.MakeAssertionAsync(clientResponse, options, creds.PublicKey, storedCounter, callback, cancellationToken: cancellationToken);

            // 6. Store the updated counter
            _db.UpdateCounter(res.CredentialId, res.Counter);

            // DEMO: now let's test the lsig to prove that delegation worked (or not)
            var origOptionsJson = HttpContext.Session.GetString("gradian.delegationSecret");
            var origOptions = AssertionOptions.FromJson(origOptionsJson);
            byte[] serverSecret = origOptions.Challenge;
            var decodedPubKey = (CborMap)CborObject.Decode(creds.PublicKey);
            byte[] pubkeyX = (byte[])decodedPubKey.GetValue(-2);
            byte[] pubkeyY = (byte[])decodedPubKey.GetValue(-3);
            var lsig = new AccountGameWallet(pubkeyX, pubkeyY);
            var compiledSig = await lsig.Compile((DefaultApi)_algodApi);
            //get the signer proxy
            var proxy = new GameWalletProxy(compiledSig);
            //TODO - the sig just needs splitting?
            proxy.ApproveTransferDelegated(clientResponse.Response.Signature, clientResponse.Response.Signature, serverSecret, roundStart, roundEnd);


            // 7. return OK to client
            return res;
        }
        catch (Exception e)
        {
            return new AssertionVerificationResult { Status = "error", ErrorMessage = FormatException(e) };
        }
    }

    private static async Task SetUpAccounts()
    {
        var accounts = await getDefaultWallet();

        //get accounts based on the above private keys using the .NET SDK
        account1 = accounts[0];
        account2 = accounts[1];
        account3 = accounts[2];
    }

    private static async Task<List<Account>> getDefaultWallet()
    {
        string handle = await getWalletHandleToken();
        var accs = await _kmdApi.ListKeysInWalletAsync(new ListKeysRequest() { Wallet_handle_token = handle });
        if (accs.Addresses.Count < 3) throw new Exception("Sandbox should offer minimum of 3 demo accounts.");

        List<Account> accounts = new List<Account>();
        foreach (var a in accs.Addresses)
        {

            var resp = await _kmdApi.ExportKeyAsync(new ExportKeyRequest() { Address = a, Wallet_handle_token = handle, Wallet_password = "" });
            Account account = new Account(resp.Private_key);
            accounts.Add(account);
        }
        return accounts;

    }

    private static async Task<string> getWalletHandleToken()
    {
        var wallets = await _kmdApi.ListWalletsAsync(null);
        var wallet = wallets.Wallets.Where(w => w.Name == walletName).FirstOrDefault();
        var handle = await _kmdApi.InitWalletHandleTokenAsync(new InitWalletHandleTokenRequest() { Wallet_id = wallet.Id, Wallet_password = "" });
        return handle.Wallet_handle_token;
    }
}