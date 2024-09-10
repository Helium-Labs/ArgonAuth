using System.Security.Cryptography;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using JWT;
using JWT.Algorithms;
using JWT.Serializers;
using Microsoft.Extensions.Caching.Memory;

namespace RelyingParty.JWT
{
    public class JwtManager
    {
        private readonly ParameterStoreService _parameterStoreService;

        public JwtManager(ParameterStoreService parameterStoreService)
        {
            _parameterStoreService = parameterStoreService;
        }

        public async Task<string> CreateTokenAsync(Dictionary<string, object> payload)
        {
            var privateKey = await GetPrivateKeyAsync();
            var publicKey = await GetPublicKeyAsync();
            Console.WriteLine("Private Key: " + privateKey);
            IJwtAlgorithm algorithm = new ES512Algorithm(publicKey, privateKey); // Private key for signing, public key (null) not required
            IJsonSerializer serializer = new JsonNetSerializer();
            IBase64UrlEncoder urlEncoder = new JwtBase64UrlEncoder();
            Console.WriteLine("Payload: " + payload);
            IJwtEncoder encoder = new JwtEncoder(algorithm, serializer, urlEncoder);
            Console.WriteLine("Encoder: " + encoder);
            // Return the encoded token
            return encoder.Encode(payload, string.Empty); // Asymmetric algorithm does not require a key
        }

        public async Task<IDictionary<string, object>> VerifyTokenAsync(string token)
        {
            var publicKey = await GetPublicKeyAsync();
            var privateKey = await GetPrivateKeyAsync();
            IJsonSerializer serializer = new JsonNetSerializer();
            IDateTimeProvider provider = new UtcDateTimeProvider();
            IJwtValidator validator = new JwtValidator(serializer, provider);
            IBase64UrlEncoder urlEncoder = new JwtBase64UrlEncoder();
            IJwtAlgorithm algorithm = new ES512Algorithm(publicKey, privateKey); // Public key for verifying, private key (null) not required
            IJwtDecoder decoder = new JwtDecoder(serializer, validator, urlEncoder, algorithm);

            // Return the verified payload
            return decoder.DecodeToObject<IDictionary<string, object>>(token);
        }
        
        // returns true if the token is valid
        public async Task<bool> VerifyJwtIsValidAsync(string token)
        {
            try
            {
                await VerifyTokenAsync(token);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Invalid JWT" + e);
                return false;
            }
        }

        private async Task<ECDsa> GetPrivateKeyAsync()
        {
            string b64EncodedPrivateKey = await _parameterStoreService.GetParameterAsync("ZKEntry-Auth-JWT-Certificate");
            var privateKeyData = Convert.FromBase64String(b64EncodedPrivateKey);
            var privateKey = ECDsa.Create();
            Console.WriteLine("Private Key: " + b64EncodedPrivateKey);
            privateKey.ImportECPrivateKey(privateKeyData, out _);
            return privateKey;
        }

        private async Task<ECDsa> GetPublicKeyAsync()
        {
            // Retrieve the private key data just as we do in GetPrivateKeyAsync
            string b64EncodedPrivateKey = await _parameterStoreService.GetParameterAsync("ZKEntry-Auth-JWT-Certificate");
            var privateKeyData = Convert.FromBase64String(b64EncodedPrivateKey);

            // Create an instance of ECDsa and import the private key
            var ecdsa = ECDsa.Create(); // The cryptographic provider
            ecdsa.ImportECPrivateKey(privateKeyData, out _);
            // Now, you can export the public key parameters from the private key
            // and create a new ECDsa instance to represent just the public key.
            var publicKeyParameters = ecdsa.ExportParameters(false); // 'false' to indicate exporting public key only
            var publicKey = ECDsa.Create();
            publicKey.ImportParameters(publicKeyParameters);

            return publicKey;
        }
    }

    public class ParameterStoreService
    {
        private readonly IAmazonSimpleSystemsManagement _ssmClient;
        private readonly IMemoryCache _cache;

        public ParameterStoreService(IAmazonSimpleSystemsManagement ssmClient, IMemoryCache cache)
        {
            _ssmClient = ssmClient;
            _cache = cache;
        }

        public async Task<string> GetParameterAsync(string parameterName)
        {
            if (!_cache.TryGetValue(parameterName, out string parameterValue))
            {
                var parameterRequest = new GetParameterRequest
                {
                    Name = parameterName,
                    WithDecryption = true
                };
                var parameterResponse = await _ssmClient.GetParameterAsync(parameterRequest);
                parameterValue = parameterResponse.Parameter.Value;

                // Set the parameter in cache with an absolute expiration relative to now
                // Tip: Customize the expiration as needed
                _cache.Set(parameterName, parameterValue, TimeSpan.FromMinutes(15));
            }

            return parameterValue;
        }
    }
}