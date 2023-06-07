using System.Numerics;
using System.Security.Cryptography;
using Fido2NetLib;

namespace RelyingParty.Utilities
{
    public static class UtilityMethods
    {
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
    }
}