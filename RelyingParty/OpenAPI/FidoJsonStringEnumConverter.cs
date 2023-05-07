using System;
using System.Text.Json;
using System.Text.Json.Serialization;


namespace RelyingParty.OpenAPI
{
    public class FidoJsonStringEnumConverter : JsonConverterFactory
    {
        private readonly JsonStringEnumConverter _innerConverter;
        private readonly Type _ignoreType;

        public FidoJsonStringEnumConverter(Type ignoreType)
        {
            _innerConverter = new JsonStringEnumConverter();
            _ignoreType = ignoreType;
        }

        public override bool CanConvert(Type typeToConvert)
        {
            return _innerConverter.CanConvert(typeToConvert) && typeToConvert != _ignoreType;
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            return _innerConverter.CreateConverter(typeToConvert, options);
        }
    }
}
