using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;

namespace RelyingParty.Utilities;

public static class CryptoHelper
{
    // Master Server Key. @TODO use AWS KMS.
    public static string masterPK = "AQnL4yIeOUCSsxbaBdvnCtzRlKHwLFPLCVsE10Y+cbo=";
    public static string masterSK = "3vRknMuz6CeLFIw2OUppPUOouWlonVIGzzeO/vIHQAg=";

    // Generate AsymmetricCipherKeyPair from master key
    public static AsymmetricCipherKeyPair GenerateMasterKeyPair()
    {
        byte[] masterPKBytes = Convert.FromBase64String(masterPK);
        byte[] masterSKBytes = Convert.FromBase64String(masterSK);
        Ed25519PrivateKeyParameters privateKey = new Ed25519PrivateKeyParameters(masterSKBytes, 0);
        Ed25519PublicKeyParameters publicKey = new Ed25519PublicKeyParameters(masterPKBytes, 0);
        return new AsymmetricCipherKeyPair(publicKey, privateKey);
    }

    public static AsymmetricCipherKeyPair GenerateX25519KeyPair()
    {
        X25519KeyPairGenerator gen = new X25519KeyPairGenerator();
        return gen.GenerateKeyPair();
    }

    // Verify the master key signed the given data
    public static bool VerifyMasterKeySignedData(byte[] data, byte[] signature)
    {
        AsymmetricCipherKeyPair masterKeyPair = GenerateMasterKeyPair();
        return Ed25519Verify(masterKeyPair.Public, data, signature);
    }

    // Sign with the master key signed the given data
    public static byte[] Ed25519SignWithMasterKey(byte[] data)
    {
        AsymmetricCipherKeyPair masterKeyPair = GenerateMasterKeyPair();
        return Ed25519Sign(masterKeyPair.Private, data);
    }

    // sign
    public static byte[] Ed25519Sign(AsymmetricKeyParameter privateKey, byte[] data)
    {
        Ed25519Signer signer = new Ed25519Signer();
        signer.Init(true, privateKey);
        signer.BlockUpdate(data, 0, data.Length);
        return signer.GenerateSignature();
    }

    // verify
    public static bool Ed25519Verify(AsymmetricKeyParameter publicKey, byte[] data, byte[] signature)
    {
        Ed25519Signer signer = new Ed25519Signer();
        signer.Init(false, publicKey);
        signer.BlockUpdate(data, 0, data.Length);
        return signer.VerifySignature(signature);
    }

    public static bool Ed25519Verify(byte[] publicKey, byte[] data, byte[] signature)
    {
        AsymmetricKeyParameter publicKeyParameter = new Ed25519PublicKeyParameters(publicKey);
        Ed25519Signer signer = new Ed25519Signer();
        signer.Init(false, publicKeyParameter);
        signer.BlockUpdate(data, 0, data.Length);
        return signer.VerifySignature(signature);
    }
}