using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.AspNetCore.Mvc;
using RelyingParty.Algorand.ServerAccount;
using RelyingParty.AlgorandFidoExtensions;
using RelyingParty.Models;
using System.Text;
using AlgoStudio.Clients;
using static Fido2NetLib.Fido2;
using Fido2NetLib.Cbor;
using Algorand.Algod;
using RelyingParty.Algorand.Signatures;
using Algorand.Algod.Model;
using Algorand;
using Algorand.Algod.Model.Transactions;
using Algorand.Utils;
using Algorand.KMD;

namespace FIDO.Handlers;

[Route("api/[controller]")]
public class FidoController : Controller
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

    private readonly ILogger _logger;

    public FidoController(ILogger<FidoController> logger, IFido2 fido2, IMasterAccount serverAccount,
        IDefaultApi algod, IApi kmdApi,
        PlanetScaleDatabase database)
    {
        _algodApi = algod;
        _fido2 = fido2;
        _serverAccount = serverAccount;
        _db = database;
        _kmdApi = (Api)kmdApi;
        _logger = logger;
    }

    private string FormatException(Exception e)
    {
        return string.Format("{0}{1}", e.Message,
            e.InnerException != null ? " (" + e.InnerException.Message + ")" : "");
    }

    // HttpPost to check if username is already registered, returning a bool. Input is just a string username in the body.
    [HttpPost]
    [Route("/usernameIsAvailable")]
    public async Task<bool> UsernameIsAvailable([FromBody] string username)
    {
        try
        {
            var user = await _db.GetUser(username);
            return user == null;
        }
        catch (Exception e)
        {
            return false;
        }
    }

    // Get request, with no parameters, that returns the current round.
    [HttpGet]
    [Route("/getLastRound")]
    public async Task<ulong> GetRound()
    {
        var status = await _algodApi.GetStatusAsync();
        return status.LastRound;
    }

    [HttpPost]
    [Route("/makeCredentialOptions")]
    public async Task<CredentialCreateOptions> MakeCredentialOptions([FromBody] MakeCredentialOptionsModel model)
    {
        try
        {
            if (string.IsNullOrEmpty(model.Username))
            {
                model.Username = $"{model.DisplayName} (Usernameless user created at {DateTime.UtcNow})";
            }
            
            // 1. Get user from DB by username (in our example, auto create missing users)
            var user = await _db.GetOrAddUser(model.Username, () => new Fido2User
            {
                DisplayName = model.DisplayName,
                Name = model.Username,
                Id = Encoding.UTF8.GetBytes(model.Username) // byte representation of userID is required
            });
            
            // 2. Get user existing keys by username
            var cred = await _db.GetCredentialsByUser(user);
            var existingKeys = cred.Select(c => c.Descriptor).ToList();

            // 3. Create options
            var authenticatorSelection = new AuthenticatorSelection
            {
                ResidentKey = model.ResidentKey.ToEnum<ResidentKeyRequirement>(),
                UserVerification = model.UserVerification.ToEnum<UserVerificationRequirement>()
            };

            if (!string.IsNullOrEmpty(model.AuthType))
                authenticatorSelection.AuthenticatorAttachment = model.AuthType.ToEnum<AuthenticatorAttachment>();

            var exts = new AuthenticationExtensionsClientInputs()
            {
                Extensions = true,
                UserVerificationMethod = true,
                DevicePubKey = new AuthenticationExtensionsDevicePublicKeyInputs() { Attestation = model.AttType },
                CredProps = true
            };

            var options = _fido2.RequestNewCredential(user, existingKeys, authenticatorSelection,
                model.AttType.ToEnum<AttestationConveyancePreference>(), exts);
            options.PubKeyCredParams = options.PubKeyCredParams.Where(o => o.Alg == COSE.Algorithm.ES256).ToList();
            // 4. Temporarily store options, session/in-memory cache/redis/db
            Dictionary<string, string> sessionJson = new Dictionary<string, string>();
            sessionJson["fido2.attestationOptions"] = options.ToJson();
            string sessionJsonString = _db.CreateJsonFromDictionary(sessionJson);
            await _db.UpdateUserJsonMetadata(model.Username, sessionJsonString);

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
    public async Task<MakeCredentialResponse> MakeCredential(string username,
        [FromBody] AuthenticatorAttestationRawResponse attestationResponse, CancellationToken cancellationToken)
    {
        try
        {
            // 1. get the options we sent the client
            string? jsonKvStore = await _db.GetUserJsonMetadata(username);
            if (jsonKvStore == null)
            {
                throw new Fido2VerificationException("No options found in database. Please check your session.");
            }

            Dictionary<string, string>? sessionDict = _db.CreateDictionaryFromJson(jsonKvStore);
            if (sessionDict == null)
            {
                throw new Fido2VerificationException(
                    "Failed to parse options from database. Please check your session.");
            }

            string? jsonOptions = sessionDict["fido2.attestationOptions"];
            if (jsonOptions == null)
            {
                throw new Fido2VerificationException("No options found in database. Please check your session.");
            }

            var options = CredentialCreateOptions.FromJson(jsonOptions);

            // 1.5 Create callback so that lib can verify credential id is unique to this user
            IsCredentialIdUniqueToUserAsyncDelegate callback = static async (args, cancellationToken) =>
            {
                var users = await _db.GetUsersByCredentialIdAsync(args.CredentialId, cancellationToken);
                if (users.Count > 0)
                    return false;

                return true;
            };

            // 2. Verify and make the credentials
            CredentialMakeResult success = await _fido2.MakeNewCredentialAsync(attestationResponse, options, callback,
                cancellationToken: cancellationToken);
            if (success.Result == null)
            {
                throw new Fido2VerificationException("Credential creation failed");
            }

            // 3. Store the credentials in db
            await _db.AddCredentialToUser(options.User, new StoredCredential
            {
                Id = success.Result.Id,
                Descriptor = new PublicKeyCredentialDescriptor(success.Result.Id),
                PublicKey = success.Result.PublicKey,
                UserHandle = success.Result.User.Id,
                SignCount = success.Result.SignCount,
                AttestationFormat = success.Result.AttestationFormat,
                RegDate = DateTime.Now,
                AaGuid = success.Result.AaGuid,
                Transports = success.Result.Transports,
                IsBackupEligible = success.Result.IsBackupEligible,
                IsBackedUp = success.Result.IsBackedUp,
                AttestationObject = success.Result.AttestationObject,
                AttestationClientDataJSON = success.Result.AttestationClientDataJson,
                DevicePublicKeys = new List<byte[]>() { success.Result.DevicePublicKey }
            });

            /*
            //get pubkey
            var decodedPubKey = (CborMap)CborObject.Decode(success.Result.PublicKey);
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
            */

            MakeCredentialResponse response = new MakeCredentialResponse()
            {
                FidoCredentialMakeResult = success,
                LogicSignatureProgram = null
            };

            return response;
        }
        catch (Exception e)
        {
            _logger.LogError("Failed to make credential: {Exception}", e);
            return new MakeCredentialResponse()
            {
                FidoCredentialMakeResult =
                    new CredentialMakeResult(status: "error", errorMessage: FormatException(e), result: null),
                // LogicSignatureProgram = null
            };
        }
    }

    /// <summary>
    /// Pre-fund the Lsig with whatever.....
    /// 
    /// TODO - Assuming this is going to be a centralised service for game developers, the particular master account, 
    /// the particular initial funding MBR and assets, would want to come from a config depending on the route.
    /// </summary>
    private async Task preFundLsig(LogicsigSignature lsigCompiled)
    {
        //DEMO CODE
        //  Just assume Sandbox here and chuck some funds at the lsig
        var transParams = await _algodApi.TransactionParamsAsync();
        var payment = PaymentTransaction.GetPaymentTransactionFromNetworkTransactionParameters(account1.Address,
            lsigCompiled.Address, 1000000, "", transParams);
        var signed = payment.Sign(account1);
        try
        {
            //send the transaction for processing
            var txRes = await ((DefaultApi)_algodApi).TransactionsAsync(new List<SignedTransaction> { signed });
            var resp = await Utils.WaitTransactionToComplete((DefaultApi)_algodApi, txRes.Txid);
        }
        catch (Algorand.ApiException<ErrorResponse> ex)
        {
            System.Diagnostics.Trace.WriteLine(ex.ToString());
        }
    }

    [HttpPost]
    [Route("/assertionOptions")]
    public async Task<AssertionOptionsResponse> AssertionOptionsPost(
        [FromBody] AssertionOptionsPostModel assertionOptions,
        string didtPubKeyAsBase64,
        string roundStartAsUINT64BEBase64,
        string roundEndAsUINT64BEBase64
    )
    {
        try
        {
            if (string.IsNullOrEmpty(assertionOptions.Username))
            {
                throw new Fido2VerificationException("Username is missing");
            }

            // 1. Get user from DB
            var user = await _db.GetUser(assertionOptions.Username) ??
                       throw new ArgumentException("Username was not registered");
            // 2. Get registered credentials from database
            var cred = await _db.GetCredentialsByUser(user);
            var existingCredentials = cred.Select(c => c.Descriptor).ToList();

            var exts = new AuthenticationExtensionsClientInputs()
            {
                Extensions = true,
                UserVerificationMethod = true,
                DevicePubKey = new AuthenticationExtensionsDevicePublicKeyInputs()
            };

            // 3. Create options
            var uv = string.IsNullOrEmpty(assertionOptions.UserVerification)
                ? UserVerificationRequirement.Discouraged
                : assertionOptions.UserVerification.ToEnum<UserVerificationRequirement>();

            var options = _fido2.GetAssertionOptions(
                existingCredentials,
                uv,
                exts
            );

            // pre-pend didtPubKeyAsBase64 to allow for the DIDT to be used as a key.
            byte[] didtPubKey = Convert.FromBase64String(didtPubKeyAsBase64);
            // create the actual DIDT token, as packed binary: didtPubKey + roundStart + roundEnd (32 + 8 + 8 = 48 bytes)
            byte[] didt = didtPubKey
                .Concat(Convert.FromBase64String(roundStartAsUINT64BEBase64))
                .Concat(Convert.FromBase64String(roundEndAsUINT64BEBase64))
                .Concat(options.Challenge)
                .ToArray();
            options.Challenge = didt;

            // 4. Temporarily store options into the DB, to check it's not a replay attack later during the assertion
            Dictionary<string, string> sessionJson = new Dictionary<string, string>();
            sessionJson["fido2.assertionOptions"] = options.ToJson();

            string sessionJsonString = _db.CreateJsonFromDictionary(sessionJson);
            await _db.UpdateUserJsonMetadata(assertionOptions.Username, sessionJsonString);

            // 5. Return options to client
            return new AssertionOptionsResponse() { FidoAssertionOptions = options, CurrentRound = 0 };
        }
        catch (Exception e)
        {
            _logger.LogError("Failed to make credential: {Exception}", e);
            return new AssertionOptionsResponse()
                { FidoAssertionOptions = new AssertionOptions { Status = "error", ErrorMessage = FormatException(e) } };
        }
    }

    [HttpPost]
    [Route("/makeAssertionAndDelegateAccess")]
    public async Task<VerifyAssertionResult> MakeAssertionAndDelegateAccess(
        [FromBody] AuthenticatorAssertionRawResponse clientResponse,
        string username,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // 1. Get the assertion options we sent the client
            string? jsonKvStore = await _db.GetUserJsonMetadata(username);
            if (jsonKvStore == null)
            {
                throw new Fido2VerificationException("No options found in database. Please check your session.");
            }

            Dictionary<string, string>? sessionDict = _db.CreateDictionaryFromJson(jsonKvStore);
            if (sessionDict == null)
            {
                throw new Fido2VerificationException(
                    "Failed to parse options from database. Please check your session.");
            }

            var jsonOptions = sessionDict["fido2.assertionOptions"];
            if (jsonOptions == null)
            {
                throw new Fido2VerificationException("No options found in database. Please check your session.");
            }

            var options = AssertionOptions.FromJson(jsonOptions);

            // 2. Get registered credential from database
            var creds = await _db.GetCredentialById(clientResponse.Id) ?? throw new Exception("Unknown credentials");

            // 3. Get credential counter from database
            var storedCounter = creds.SignCount;

            // 4. Create callback to check if userhandle owns the credentialId
            IsUserHandleOwnerOfCredentialIdAsync callback = static async (args, cancellationToken) =>
            {
                var storedCreds = await _db.GetCredentialsByUserHandleAsync(args.UserHandle, cancellationToken);
                return storedCreds.Exists(c => c.Descriptor.Id.SequenceEqual(args.CredentialId));
            };

            // 5. Make the assertion
            var res = await _fido2.MakeAssertionAsync(clientResponse, options, creds.PublicKey,
                creds.DevicePublicKeys, storedCounter, callback, cancellationToken);

            // Extract the challenge and record it as their session token for access delegation.
            // Expiration dictated by challenge encoded DIDT token.
            byte[] didt = options.Challenge;
            byte[] signature = clientResponse.Response.Signature;
            await _db.UpsertDidt(clientResponse.Id, didt, signature);

            // 6. Store the updated counter
            await _db.UpdateCounter(res.CredentialId, res.SignCount);

            if (res.DevicePublicKey is not null)
            {
                // verify res.DevicePublicKey doesn't already exist in creds.DevicePublicKeys 
                if (!creds.DevicePublicKeys.Any(x => x.SequenceEqual(res.DevicePublicKey)))
                {
                    // doesn't exist, so add it as another device belonging to the credential (passkey)
                    creds.DevicePublicKeys.Add(res.DevicePublicKey);
                    await _db.UpdateDevicePublicKeys(res.CredentialId, creds.DevicePublicKeys);
                }
            }

            // DEMO: now let's test the lsig to prove that delegation worked (or not)
            byte[] serverSecret = options.Challenge;
            // ES256 Credential Public Key Extraction
            var decodedPubKey = (CborMap)CborObject.Decode(creds.PublicKey);
            // X and Y values represent the coordinates of a point on the elliptic curve, constituting the public key
            byte[] pubkeyX = (byte[])decodedPubKey.GetValue(-2);
            byte[] pubkeyY = (byte[])decodedPubKey.GetValue(-3);
            var lsig = new AccountGameWallet(pubkeyX, pubkeyY);
            var compiledSig = await lsig.Compile((DefaultApi)_algodApi);

            // get the signer proxy
            // GameWalletProxy proxy = new Proxies.GameWalletProxy(compiledSig);
            // send funding to the proxy

            // 7. return OK to client
            return res;
        }
        catch (Exception e)
        {
            return new VerifyAssertionResult { Status = "error", ErrorMessage = FormatException(e) };
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
            var resp = await _kmdApi.ExportKeyAsync(new ExportKeyRequest()
                { Address = a, Wallet_handle_token = handle, Wallet_password = "" });
            Account account = new Account(resp.Private_key);
            accounts.Add(account);
        }

        return accounts;
    }

    private static async Task<string> getWalletHandleToken()
    {
        var wallets = await _kmdApi.ListWalletsAsync(null);
        var wallet = wallets.Wallets.Where(w => w.Name == walletName).FirstOrDefault();
        var handle = await _kmdApi.InitWalletHandleTokenAsync(new InitWalletHandleTokenRequest()
            { Wallet_id = wallet.Id, Wallet_password = "" });
        return handle.Wallet_handle_token;
    }
}