using SampleClient.Data;

namespace GameServer
{
    public partial class RelyingParty
    {
        partial void UpdateJsonSerializerSettings(Newtonsoft.Json.JsonSerializerSettings settings)
        {
            settings.Converters.Add(new Base64UrlConverter());
            settings.Error = (sender, args) =>
            {
                if (System.Diagnostics.Debugger.IsAttached)
                {
                    System.Diagnostics.Debugger.Break();
                }
            };
        }
    }
}
