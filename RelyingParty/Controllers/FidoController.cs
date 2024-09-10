using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.AspNetCore.Mvc;
using RelyingParty.AlgorandFidoExtensions;
using RelyingParty.Data;
using RelyingParty.Models;
using RelyingParty.Utilities;
using System.Text;
using Newtonsoft.Json;
using RelyingParty.JWT;
using static Fido2NetLib.Fido2;

namespace FIDO.Handlers;

[Route("api/[controller]")]
public class FidoController : Controller
{
    private static PlanetScaleDatabase _db;

    private readonly ILogger _logger;
    private readonly JwtManager _jwtManager;
    private readonly IEmailService _emailService;

    private const string AssertionOptionsKey = "fido2.assertionOptions";
    private const string AttestationOptionsKey = "fido2.attestationOptions";

    public FidoController(
        ILogger<FidoController> logger,
        PlanetScaleDatabase database,
        JwtManager jwtManager,
        IEmailService emailService)
    {
        _db = database;
        _logger = logger;
        _jwtManager = jwtManager;
        _emailService = emailService;
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
            // print out user
            return user == null;
        }
        catch (Exception e)
        {
            Console.WriteLine("Exception: " + e);
            return false;
        }
    }
    
    // HttpPost to check if username is already registered, returning a bool. Input is just a string username in the body.
    [HttpPost]
    [Route("/tokenIsValid")]
    public async Task<bool> TokenIsValid([FromBody] string jwt)
    {
        try
        {
            return await _jwtManager.VerifyJwtIsValidAsync(jwt);
        }
        catch (Exception e)
        {
            Console.WriteLine("Exception: " + e);
            return false;
        }
    }

    [HttpPost]
    [Route("/makeCredentialOptions")]
    public async Task<CredentialOptionsModel> MakeCredentialOptions([FromBody] MakeCredentialOptionsModel model,
        string clientSessionKeyB64,
        CancellationToken cancellationToken)
    {
        try
        {
            var _fido2 = UtilityMethods.GetFido2(Request.Headers["origin"]);
            if (string.IsNullOrEmpty(model.Username))
            {
                model.Username = $"{model.DisplayName} (Usernameless user created at {DateTime.UtcNow})";
            }

            // 1. Get or add user from/to the database by username
            var user = await _db.GetOrAddUser(model.Username, () => new Fido2User
            {
                DisplayName = model.Username,
                Name = model.Username,
                Id = Encoding.UTF8.GetBytes(model.Username) // byte representation of userID is required
            });

            // 2. Get user existing keys by username
            var existingCredentials = (await _db.GetCredentialsByUser(user, cancellationToken))
                .Select(c => c.Descriptor).ToList();

            // 3. Create options
            var authenticatorSelection = new AuthenticatorSelection
            {
                ResidentKey = model.ResidentKey.ToEnum<ResidentKeyRequirement>(),
                UserVerification = model.UserVerification.ToEnum<UserVerificationRequirement>()
            };

            if (!string.IsNullOrEmpty(model.AuthType))
                authenticatorSelection.AuthenticatorAttachment = model.AuthType.ToEnum<AuthenticatorAttachment>();

            var exts = new AuthenticationExtensionsClientInputs
            {
                Extensions = true,
                UserVerificationMethod = true,
                DevicePubKey = new AuthenticationExtensionsDevicePublicKeyInputs { Attestation = model.AttType },
                CredProps = true
            };

            var options = _fido2.RequestNewCredential(user, existingCredentials, authenticatorSelection,
                model.AttType.ToEnum<AttestationConveyancePreference>(), exts);

            // Filter to supported algorithms
            options.PubKeyCredParams = options.PubKeyCredParams.Where(o => o.Alg is
                COSE.Algorithm.ES256 or
                COSE.Algorithm.RS256
            ).ToList();

            // Generate & return Decentralized Web Token (DWT) for authorizing access to centralized services,
            DWT dwt = new DWT(clientSessionKeyB64, user.Name);
            options.Challenge = dwt.hash;

            // 4. Temporarily store options in user's metadata
            var sessionJson = new Dictionary<string, string>();
            sessionJson.Add(AttestationOptionsKey, options.ToJson());
            var sessionJsonString = JsonConvert.SerializeObject(sessionJson);
            await _db.UpdateUserJsonMetadata(model.Username, sessionJsonString);

            // 5. Insert email verification and send email, similar to above...
            // Insert email verification
            var emailCode = UtilityMethods.GenerateSixDigitCode();
            await _db.InsertEmailVerification(model.Username, emailCode);
            // Send email to user with verification code
            var origin = Request.Headers["origin"];
            var emailSent = await _emailService.SendEmailAsync(
                model.Username,
                "Confirm your account",
                EmailTemplater.GenerateVerifyEmailCode(origin, emailCode)
            );
            if (!emailSent)
            {
                throw new Fido2VerificationException("Failed to send email");
            }

            // 6. Return options and DWT to the client
            return new CredentialOptionsModel { Options = options, DWT = dwt };
        }
        catch (Exception e)
        {
            _logger.LogError("Exception creating credential options: {Exception}", e);
            return new CredentialOptionsModel { Status = "error", ErrorMessage = FormatException(e) };
        }
    }

    // Helper
    private static async Task<bool> EmailCodeIsValid(string email, string code)
    {
        // Retrieve the email verification entry from the database
        var verificationEntry = await _db.GetEmailVerification(email);
        if (verificationEntry == null)
        {
            // No email verification entry found
            return false;
        }

        // Hash the provided code
        var providedCodeHash = UtilityMethods.ToBase64String(
            UtilityMethods.ComputeSHA256Hash(
                Encoding.UTF8.GetBytes(code)
            ));
        // Check if the hashed code matches the one in the database
        return providedCodeHash == verificationEntry.CodeHash;
    }

    // At the global level, we will impose IP throttling.
    [HttpPost]
    [Route("/verifyEmailCode")]
    public async Task<bool> VerifyEmailCode([FromBody] VerifyEmailCodeRequestModel request)
    {
        try
        {
            return await EmailCodeIsValid(request.Email, request.Code);
        }
        catch (Exception e)
        {
            _logger.LogError("Exception verifying email code: {Exception}", e);
            return false;
        }
    }
    private static async Task<string> CreateAndInsertAuthExchange(
        string username,
        Dictionary<string, object> jwtClaims,
        PlanetScaleDatabase db,
        string state,
        string codeChallenge
    )
    {
        string code = UtilityMethods.GenerateSecureNonce(32);
        string codeHashB64 = UtilityMethods.ToBase64String(UtilityMethods.ComputeSHA256Hash(Convert.FromBase64String(code)));

        await db.InsertAuthExchange(
            username,
            jwtClaims,
            codeHashB64,
            state,
            codeChallenge
        );

        return code;
    }
    
    [HttpPost]
    [Route("/makeCredential")]
    public async Task<MakeCredentialResponse> MakeCredential(
        [FromBody] MakeCredentialsRequestModel credentialsRequestBody,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var _fido2 = UtilityMethods.GetFido2(Request.Headers["origin"]);
            // 0. Check if they have verified their email
            var emailCodeIsValid =
                await EmailCodeIsValid(credentialsRequestBody.username, credentialsRequestBody.emailCode);
            if (!emailCodeIsValid)
            {
                throw new Fido2VerificationException("Email code is not valid");
            }
            var username = credentialsRequestBody.username;
            var attestationResponse = credentialsRequestBody.attestationResponse;
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

            string? jsonOptions = sessionDict[AttestationOptionsKey];
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
            var devicePublicKey = new List<byte[]>() { };
            if (success.Result.DevicePublicKey != null)
            {
                devicePublicKey.Add(success.Result.DevicePublicKey);
            }

            await _db.AddCredentialToUser(new StoredCredential
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
                DevicePublicKeys = devicePublicKey
            });
            
            var dwt = credentialsRequestBody.dwt;
            dwt.SetCredPk(success.Result.PublicKey);
            
            // Store the JWT into the database, keyed by code
            var code = await CreateAndInsertAuthExchange(
                username,
                dwt.ToDictionary(),
                _db,
                credentialsRequestBody.state,
                credentialsRequestBody.codeChallenge
            );

            return new MakeCredentialResponse()
            {
                FidoCredentialMakeResult = success,
                Code = code
            };
        }
        catch (Exception e)
        {
            _logger.LogError("Failed to make credential: {Exception}", e);
            return new MakeCredentialResponse()
            {
                FidoCredentialMakeResult =
                    new CredentialMakeResult(status: "error", errorMessage: FormatException(e), result: null),
            };
        }
    }

    [HttpPost]
    [Route("/assertionOptions")]
    public async Task<AssertionOptionsResponse> AssertionOptionsPost(
        [FromBody] AssertOptionsRequestModel assertionOptions,
        string clientSessionKeyB64,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var _fido2 = UtilityMethods.GetFido2(Request.Headers["origin"]);
            if (string.IsNullOrEmpty(assertionOptions.Username))
            {
                throw new Fido2VerificationException("Username is missing");
            }

            // 1. Get user from DB
            var user = await _db.GetUser(assertionOptions.Username) ??
                       throw new ArgumentException("Username was not registered");

            // 2. Get registered credentials from database
            var cred = await _db.GetCredentialsByUser(user, cancellationToken);
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

            // Generate Decentralized Web Token (DWT) for authorizing access to centralized services,
            // in a replay attack proof way.
            DWT dwt = new DWT(clientSessionKeyB64, user.Name);
            options.Challenge = dwt.hash;

            // 4. Temporarily store options into the DB, to check it's not a replay attack later during the assertion
            Dictionary<string, string> sessionJson = new Dictionary<string, string>();
            sessionJson[AssertionOptionsKey] = options.ToJson();

            string sessionJsonString = _db.CreateJsonFromDictionary(sessionJson);
            await _db.UpdateUserJsonMetadata(assertionOptions.Username, sessionJsonString);

            // 5. Return options to client
            return new AssertionOptionsResponse() { FidoAssertionOptions = options, DWT = dwt };
        }
        catch (Exception e)
        {
            _logger.LogError("Failed to make credential: {Exception}", e);
            return new AssertionOptionsResponse()
                { FidoAssertionOptions = new AssertionOptions { Status = "error", ErrorMessage = FormatException(e) } };
        }
    }

    /**
     * Make Webauthn Assertion
     */
    [HttpPost]
    [Route("/makeAssertion")]
    public async Task<AssertionResponseModel> MakeAssertion(
        [FromBody] MakeAssertionRequestModel makeAssertionRequestBody,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var _fido2 = UtilityMethods.GetFido2(Request.Headers["origin"]);
            var clientResponse = makeAssertionRequestBody.clientResponse;
            var username = makeAssertionRequestBody.username;
            var dwt = makeAssertionRequestBody.dwt;
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

            var jsonOptions = sessionDict[AssertionOptionsKey];
            if (jsonOptions == null)
            {
                throw new Fido2VerificationException("No options found in database. Please check your session.");
            }

            var options = AssertionOptions.FromJson(jsonOptions);

            // 2. Get registered credential from database
            var cred = await _db.GetCredentialById(clientResponse.Id) ?? throw new Exception("Unknown credentials");

            // 3. Get credential counter from database
            var storedCounter = cred.SignCount;

            // 4. Create callback to check if userhandle owns the credentialId
            IsUserHandleOwnerOfCredentialIdAsync callback = static async (args, cancellationToken) =>
            {
                var storedCreds = await _db.GetCredentialsByUserHandleAsync(args.UserHandle, cancellationToken);
                return storedCreds.Exists(c => c.Descriptor.Id.SequenceEqual(args.CredentialId));
            };

            // 5. Make the assertion
            var res = await _fido2.MakeAssertionAsync(clientResponse, options, cred.PublicKey,
                cred.DevicePublicKeys, storedCounter, callback, cancellationToken);

            // Check dwt.hash and options.Challenge are equal values, to prove the DWT wasn't tampered with.
            if (!UtilityMethods.AreByteArraysEqual(dwt.hash, options.Challenge))
            {
                throw new Fido2VerificationException("DWT hash and options.Challenge are not equal");
            }

            // Passed verification, so we can now sign the DWT with the server account.
            dwt.SetCredPk(cred.PublicKey);
            dwt.credSig = makeAssertionRequestBody.clientResponse.Response.Signature;
            dwt.authenticatorData = makeAssertionRequestBody.clientResponse.Response.AuthenticatorData;
            dwt.clientDataJSON = makeAssertionRequestBody.clientResponse.Response.ClientDataJson;
            // Check validity of DWT. I.e. it has been approved by the client (csPK) and the server (rpSig).
            if (!dwt.IsValid())
            {
                throw new Fido2VerificationException("DWT is not valid");
            }

            // 6. Store the updated counter
            await _db.UpdateCounter(res.CredentialId, res.SignCount);

            if (res.DevicePublicKey is not null)
            {
                // verify res.DevicePublicKey doesn't already exist in creds.DevicePublicKeys
                if (!cred.DevicePublicKeys.Any(x => x.SequenceEqual(res.DevicePublicKey)))
                {
                    // doesn't exist, so add it as another device belonging to the credential (passkey)
                    cred.DevicePublicKeys.Add(res.DevicePublicKey);
                    await _db.UpdateDevicePublicKeys(res.CredentialId, cred.DevicePublicKeys);
                }
            }

            // Store the JWT into the database, keyed by code
            var code = await CreateAndInsertAuthExchange(
                username,
                dwt.ToDictionary(),
                _db,
                makeAssertionRequestBody.state,
                makeAssertionRequestBody.codeChallenge
            );

            // 7. return OK to client
            return new AssertionResponseModel()
            {
                VerifyAssertionResult = res,
                Code = code
            };
        }
        catch (Exception e)
        {
            return new AssertionResponseModel { Status = "error", ErrorMessage = FormatException(e) };
        }
    }

    // HttpPost to exchange a code for a JWT. Input is just a string code in the body.
    [HttpPost]
    [Route("/exchangeCodeForJWT")]
    public async Task<string> ExchangeCodeForJwt([FromBody] GetExchangeCodeRequestModel requestParams)
    {
        try
        {
            // Verify the code is valid, state and PKCE code challenge are equal
            var code = requestParams.code;
            var state = requestParams.state;
            var pkceCodeVerifier = requestParams.codeVerifier;

            // Hash it again
            string codeSha256 = UtilityMethods.ToBase64String(
                UtilityMethods.ComputeSHA256Hash(
                    Convert.FromBase64String(code)
                ));
            var authExchange = await _db.GetAuthExchangeIfValid(codeSha256);
            if (authExchange == null)
            {
                throw new Fido2VerificationException("No auth exchange found in database. Please check your session.");
            }

            // Check the state is equal
            if (authExchange.State != state)
            {
                throw new Fido2VerificationException("State is not equal");
            }

            // Check the PKCE code challenge is equal
            var pkceCodeChallenge = UtilityMethods.ToBase64String(
                UtilityMethods.ComputeSHA256Hash(
                    Convert.FromBase64String(pkceCodeVerifier)
                ));
            if (authExchange.CodeChallenge != pkceCodeChallenge)
            {
                throw new Fido2VerificationException("PKCE code challenge is not equal");
            }

            // @TODO: implement client id and client secret verification. Origin must be expected.
            var claims = JsonConvert.DeserializeObject<Dictionary<string, object>>(authExchange.JwtClaims);
            if (claims == null)
            {
                throw new Fido2VerificationException(
                    "Failed to parse JWT claims from database. Please check your session.");
            }

            var jwt = await _jwtManager.CreateTokenAsync(claims);
            return jwt;
        }
        catch (Exception e)
        {
            Console.WriteLine("Exception: " + e);
            return "error";
        }
    }
}