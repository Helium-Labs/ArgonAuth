using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using System;
using GameServer;

namespace SampleClient.Data
{
    public class FidoLibEnumConverter : JsonConverter
    {

        public override bool CanConvert(Type objectType)
        {
            var type = Nullable.GetUnderlyingType(objectType) ?? objectType;

            return type.IsEnum;
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(value);
            
        }

        

    }
}
