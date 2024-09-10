using RelyingParty.Utilities;
using System.Text;
using System.Text.Json;

namespace RelyingParty.Models;

// A valid DWT must be signed by the credential that it belongs to.
public class DWT
{
    // Client Session Public Key
    public byte[] cspk { get; set; }

    // Webauthn Authenticator Public Key
    public byte[]? credpk { get; set; }

    // Expiration Time in Unix Seconds
    public ulong exp { get; set; }

    // Username of user that signed the DIDT (hash as the challenge)
    public string user { get; set; }

    // Random 32 byte Nonce (base64 encoded)
    public string rand { get; set; }

    // A B64 SHA256 hash of the DWT
    public byte[] hash => Hash();
    
    // ASN1DEREncodedSignature Assertion signature (only during assertion, filled client side only)
    public byte[]? credSig { get; set; }
    
    // Autheneticator data authenticatorData (only during assertion, filled client side only)
    public byte[]? authenticatorData { get; set; }
    
    // clientDataJSON (only during assertion, filled client side only)
    public byte[]? clientDataJSON { get; set; }
    
    // Initializer
    public DWT(byte[] cspk, ulong exp, string user, byte[] credId, string rand)
    {
        this.cspk = cspk;
        this.exp = exp;
        this.user = user;
        this.rand = rand;
    }

    // Initializer supplying only cspk and credentialID
    public DWT(byte[] cspk, string user, byte[] credId)
    {
        this.cspk = cspk;
        this.user = user;
        // Generate JWT lifetime of 12 hours by default
        var expUnixSeconds = UtilityMethods.GetCurrentUnixTimeInSeconds();
        expUnixSeconds += 43200;
        exp = expUnixSeconds;

        // Generate random 32 byte nonce
        rand = UtilityMethods.GenerateSecureNonce(32);
    }

    // Initializer supplying only cspk and credentialID
    public DWT(string cspkB64, string user)
    {
        cspk = Convert.FromBase64String(cspkB64);
        this.user = user;
        // Generate JWT lifetime of 12 hours by default
        var expUnixSeconds = UtilityMethods.GetCurrentUnixTimeInSeconds();
        expUnixSeconds += 43200;
        exp = expUnixSeconds;

        // Generate random 32 byte nonce
        rand = UtilityMethods.GenerateSecureNonce(32);
    }

    public DWT()
    {
        // Parameterless constructor
    }

    // B64 Sha256 Hash of the JWT, in a reproducible way.
    public byte[] Hash()
    {
        // Hash the DIDT
        var dwt = Array.Empty<byte>();
        dwt = dwt.Concat(cspk).ToArray();
        dwt = dwt.Concat(UtilityMethods.EncodeUint64(exp)).ToArray();
        dwt = dwt.Concat(Encoding.UTF8.GetBytes(user)).ToArray();
        dwt = dwt.Concat(Convert.FromBase64String(rand)).ToArray();
        // To base64 string
        return UtilityMethods.ComputeSHA256Hash(dwt);
    }

    public void SetCredPk(byte[] credPk)
    {
        credpk = credPk;
    }

    // Verify if this DWT (its hash) has been signed by the RP and CSPK
    public bool IsValid()
    {
        // Check fields are not null
        if (cspk == null || exp == 0 || user == null || rand == null)
        {
            return false;
        }
        // check exp is no more than 12 hours in the future
        var now = UtilityMethods.GetCurrentUnixTimeInSeconds();
        if (exp < now || exp > now + 43200)
        {
            return false;
        }

        return true;
    }

    // From JSON Base64 encoded string, for transport in the bearer header.
    public static DWT FromJson(string jsonB64)
    {
        var json = Encoding.UTF8.GetString(Convert.FromBase64String(jsonB64));
        var result = JsonSerializer.Deserialize<DWT>(json);
        if (result == null)
        {
            throw new Exception("Can't parse DWT");
        }

        return result;
    }

    // To JSON B64 encoded string, for transport in the bearer header.
    public string ToJsonB64()
    {
        var json = JsonSerializer.Serialize(this);
        return UtilityMethods.ToBase64String(Encoding.UTF8.GetBytes(json));
    }
    
    // Return Dictionary<string, object> for JWT construction
    public Dictionary<string, object> ToDictionary()
    {
        var dict = new Dictionary<string, object>();
        dict.Add("cspk", cspk);
        dict.Add("exp", exp);
        dict.Add("user", user);
        dict.Add("rand", rand);
        dict.Add("hash", hash);
        dict.Add("credpk", credpk);
        dict.Add("credSig", credSig);
        dict.Add("authenticatorData", authenticatorData);
        dict.Add("clientDataJSON", clientDataJSON);
        return dict;
    }
}