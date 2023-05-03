using Newtonsoft.Json.Converters;
using SampleClient.Data;

namespace GameServer
{
    public partial class RelyingParty
    {
        partial void UpdateJsonSerializerSettings(Newtonsoft.Json.JsonSerializerSettings settings)
        {
            settings.Converters.Add(new Base64UrlConverter()); //Is this still necessary?
          //  settings.Converters.Add(new FidoLibEnumConverter()); //Fido lib uses different enum values depending on if reading or writing ffs :-(
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
