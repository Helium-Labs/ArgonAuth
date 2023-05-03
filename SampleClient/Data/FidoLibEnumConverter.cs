using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using System;
using GameServer;

namespace SampleClient.Data
{
    public class FidoLibEnumConverter : StringEnumConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is Algorithm) // MyEnum is the enum you want to handle differently
            {
                writer.WriteValue((int)value); // write the integer value instead of the string value
            }
            else
            {
                base.WriteJson(writer, value, serializer); // use the default behavior for other enums
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (objectType == typeof(Algorithm)) // MyEnum is the enum you want to handle differently
            {
                return (Algorithm)Convert.ToInt32(reader.Value); // read the integer value and convert it to MyEnum
            }
            else
            {
                return base.ReadJson(reader, objectType, existingValue, serializer); // use the default behavior for other enums
            }
        }

    }
}
