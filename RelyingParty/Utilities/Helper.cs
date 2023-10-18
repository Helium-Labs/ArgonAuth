using System.Security.Cryptography;
using System.Text;
using Fido2NetLib;

namespace RelyingParty.Utilities
{
    public static class UtilityMethods
    {
        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static byte[] GetSessionPublicKeyFromDidt(byte[] didt)
        {
            return didt.Take(32).ToArray();
        }

        // big endian encoded int of 8 bytes
        public static ulong GetRVStart(byte[] didt)
        {
            byte[] rvStartBytes = didt.Skip(32).Take(8).ToArray();
            ulong rvStart = ConvertByteArrayToULong(rvStartBytes);
            return rvStart;
        }

        // big endian encoded int of 8 bytes
        public static ulong GetRVEnd(byte[] didt)
        {
            byte[] rvEndBytes = didt.Skip(40).Take(8).ToArray();
            ulong rvEnd = ConvertByteArrayToULong(rvEndBytes);
            return rvEnd;
        }

        public static ulong ConvertByteArrayToULong(byte[] bytes)
        {
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }

            return BitConverter.ToUInt64(bytes, 0);
        }

        public static byte[] GetRandomBytesFromDidt(byte[] didt)
        {
            // the remaining bytes once the first *+8+32=48 bytes are removed
            return didt.Skip(48).ToArray();
        }

        public static byte[] GetRValueFromSignature(byte[] signature)
        {
            var decoded = Asn1Element.Decode(signature);
            var r = decoded[0].GetIntegerBytes();
            return r.ToArray();
        }

        public static byte[] GetSValueFromSignature(byte[] signature)
        {
            var decoded = Asn1Element.Decode(signature);
            var s = decoded[1].GetIntegerBytes();
            return s.ToArray();
        }

        public static ulong GetCurrentUnixTimeInSeconds()
        {
            return (ulong)(DateTime.UtcNow - UnixEpoch).TotalSeconds;
        }

        public static string GenerateSecureNonce(int length)
        {
            byte[] randomNumber = new byte[length];

            // Fill the buffer with a random value
            RandomNumberGenerator.Fill(randomNumber);

            // Convert to a Base64 string for easier handling and transport
            return Convert.ToBase64String(randomNumber);
        }

        // to base64 string
        public static string ToBase64String(byte[] bytes)
        {
            return Convert.ToBase64String(bytes);
        }

        public static byte[] ComputeSHA256Hash(string input)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                // Convert the input string to bytes
                byte[] bytes = Encoding.UTF8.GetBytes(input);

                // Compute the SHA-256 hash
                byte[] hashBytes = sha256.ComputeHash(bytes);

                return hashBytes;
            }
        }

        public static bool AreByteArraysEqual(byte[] a1, byte[] a2)
        {
            if (a1 == null || a2 == null)
                return false;

            if (a1.Length != a2.Length)
                return false;

            for (int i = 0; i < a1.Length; i++)
            {
                if (a1[i] != a2[i])
                    return false;
            }

            return true;
        }
    }
}