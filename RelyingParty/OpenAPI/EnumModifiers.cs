using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;
using System.Runtime.Serialization;

namespace RelyingParty.OpenAPI
{
    public class EnumSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema model, SchemaFilterContext context)
        {
            if (context.Type.IsEnum)
            {
                var enumNames = new OpenApiArray();
                foreach (var name in Enum.GetNames(context.Type))
                {
                    var field = context.Type.GetField(name);
                    var attribute = field.GetCustomAttribute<EnumMemberAttribute>();
                    enumNames.Add(new OpenApiString(attribute?.Value ?? name));
                }
                model.Extensions.Add("x-enumNames", enumNames);
            }
        }
    }
}
