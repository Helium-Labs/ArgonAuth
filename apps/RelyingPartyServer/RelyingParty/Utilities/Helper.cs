using Fido2NetLib;
using System.Security.Cryptography;

namespace RelyingParty.Utilities
{
    public static class UtilityMethods
    {
        private static readonly Random random = new Random();
        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private static readonly Dictionary<string, Fido2> _fido2Instances = new();

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

        public static string Base64UrlToBase64(string base64Url)
        {
            string base64 = base64Url
                .Replace('-', '+')
                .Replace('_', '/');

            // Pad with trailing '='s if necessary
            switch (base64.Length % 4)
            {
                case 2:
                    base64 += "==";
                    break;
                case 3:
                    base64 += "=";
                    break;
            }

            return base64;
        }

        public static string Base64ToBase64Url(string base64)
        {
            string base64Url = base64
                .Replace('+', '-') // Replace '+' with '-'
                .Replace('/', '_') // Replace '/' with '_'
                .TrimEnd('='); // Remove any trailing '=' padding characters

            return base64Url;
        }

        public static byte[] ComputeSHA256Hash(byte[] bytes)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                // Compute the SHA-256 hash
                byte[] hashBytes = sha256.ComputeHash(bytes);

                return hashBytes;
            }
        }
        public static string GenerateSixDigitCode()
        {
            // Initialize a 6-character long array to hold the code
            char[] code = new char[6];

            // Use the space of alphabetic characters (a-z) and numeric characters (0-9)
            for (int i = 0; i < code.Length; i++)
            {
                // Generate a random number between 0 and 35
                int num = random.Next(36);

                // If the number is less than 26, use the space of alphabetic characters
                if (num < 26)
                {
                    // Convert the number to its corresponding alphabetic character
                    code[i] = (char)('a' + num);
                }
                // Otherwise, use the space of numeric characters
                else
                {
                    // Convert the number to its corresponding numeric character
                    code[i] = (char)('0' + num - 26);
                }
            }

            // Convert the character array to a string and return it
            return new string(code);
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

        public static byte[] EncodeUint64(ulong num)
        {
            byte[] bytes = BitConverter.GetBytes(num);

            // BitConverter returns bytes in little-endian on little-endian systems,
            // so we reverse it to ensure big-endian format.
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }

            return bytes;
        }
        
        public static Fido2 GetFido2(string origin)
        {
            // Check _fido2Instances for an existing instance
            if (_fido2Instances.TryGetValue(origin, out var fido2Instance))
            {
                return fido2Instance;
            }
            // Log the origin. Origin includes the protocol, host, and port.
            var host = origin.Split(":")[1].Substring(2);
            // If no instance exists, create a new one
            fido2Instance = new Fido2(new Fido2Configuration{
                ServerDomain = host,
                ServerName = "FIDO2 Server",
                Origins = new HashSet<string>{origin},
                TimestampDriftTolerance = 300000,
            });
            // Add the new instance to the map
            _fido2Instances.Add(origin, fido2Instance);
            return fido2Instance;
        }
    }
}