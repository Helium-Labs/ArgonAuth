using Fido2NetLib;
using Fido2NetLib.Development;
using Fido2NetLib.Objects;
using Microsoft.AspNetCore.Mvc;
using RelyingParty.Algorand.ServerAccount;
using RelyingParty.AlgorandFidoExtensions;
using RelyingParty.Models;
using System.Text;
using System.Linq;
using static Fido2NetLib.Fido2;
using System.Configuration;

namespace Fido2Demo;

[Route("api/[controller]")]
public class Fido2Controller : Controller
{
    private IFido2 _fido2;
    private IMasterAccount _serverAccount;
    public static IMetadataService _mds;
    public static PlanetScaleDatabase _db;

    public Fido2Controller(IFido2 fido2, IMasterAccount serverAccount, PlanetScaleDatabase database)
    {
        _fido2 = fido2;
        _serverAccount = serverAccount;
        _db = database;

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
            _db.AddCredentialToUser(options.User, new StoredCredential
            {
                Descriptor = new PublicKeyCredentialDescriptor(success.Result.CredentialId),
                PublicKey = success.Result.PublicKey,
                UserHandle = success.Result.User.Id,
                SignatureCounter = success.Result.Counter,
                CredType = success.Result.CredType,
                RegDate = DateTime.UtcNow,
                AaGuid = success.Result.Aaguid
            });

            // Remove Certificates from success because System.Text.Json cannot serialize them properly. See https://github.com/passwordless-lib/fido2-net-lib/issues/328
            success.Result.AttestationCertificate = null;
            success.Result.AttestationCertificateChain = null;


            //Modify our logic signature


            MakeCredentialResponse response = new MakeCredentialResponse()
            {
                FidoCredentialMakeResult = success,
                 
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

    [HttpPost]
    [Route("/assertionOptions")]
    public AssertionOptions AssertionOptionsPost( AssertionOptionsPostModel assertionOptions)
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

            // 4. Temporarily store options, session/in-memory cache/redis/db
            HttpContext.Session.SetString("fido2.assertionOptions", options.ToJson());

            // 5. Return options to client
            return options;
        }
        catch (Exception e)
        {
            return new AssertionOptions { Status = "error", ErrorMessage = FormatException(e) };
        }
    }

    [HttpPost]
    [Route("/makeAssertion")]
    public async Task<AssertionVerificationResult> MakeAssertion( AuthenticatorAssertionRawResponse clientResponse, CancellationToken cancellationToken)
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
            var res = await _fido2.MakeAssertionAsync(clientResponse, options, creds.PublicKey, storedCounter, callback, cancellationToken: cancellationToken);

            // 6. Store the updated counter
            _db.UpdateCounter(res.CredentialId, res.Counter);

            // 7. return OK to client
            return res;
        }
        catch (Exception e)
        {
            return new AssertionVerificationResult { Status = "error", ErrorMessage = FormatException(e) };
        }
    }
}