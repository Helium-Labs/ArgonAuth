

using Newtonsoft.Json;

namespace SampleClient.Data
{

    // Define a custom converter for byte arrays
    public class ByteArrayConverterForJS : JsonConverter<byte[]>
    {
        public override byte[] ReadJson(JsonReader reader, Type objectType, byte[] existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            // Read the JSON array and convert it to a byte array
            var list = new List<byte>();
            while (reader.Read() && reader.TokenType != JsonToken.EndArray)
            {
                list.Add(Convert.ToByte(reader.Value));
            }
            return list.ToArray();
        }

        public override void WriteJson(JsonWriter writer, byte[] value, JsonSerializer serializer)
        {
            // Write the byte array as a JSON array
            writer.WriteStartArray();
            foreach (var b in value)
            {
                writer.WriteValue(b);
            }
            writer.WriteEndArray();
        }
    }
}
