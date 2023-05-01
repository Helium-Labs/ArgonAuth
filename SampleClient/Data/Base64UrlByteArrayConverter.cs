using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using System.Text.Json;

namespace SampleClient.Data
{
    public class Base64UrlConverter : JsonConverter<byte[]>
    {
        public override byte[]? ReadJson(JsonReader reader, Type objectType, byte[]? existingValue, bool hasExistingValue, Newtonsoft.Json.JsonSerializer serializer)
        {
            // Convert base64url string to byte array
            string base64url = (string)reader.Value;
            string base64 = base64url.Replace('-', '+').Replace('_', '/'); // Convert to regular base64
            int pad = base64.Length % 4; // Add padding if needed
            if (pad > 0)
            {
                base64 += new string('=', 4 - pad);
            }
            return Convert.FromBase64String(base64);
        }

        public override void WriteJson(JsonWriter writer, byte[]? value, Newtonsoft.Json.JsonSerializer serializer)
        {
            // Convert byte array to base64url string
            string base64 = Convert.ToBase64String(value);
            string base64url = base64.Replace('+', '-').Replace('/', '_'); // Convert to base64url
            base64url = base64url.TrimEnd('='); // Remove padding if any
            writer.WriteValue(base64url);
        }
    }
}
