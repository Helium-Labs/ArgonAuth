using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using SampleClient.Data;

namespace GameServer
{


    /// <summary>
    /// IMPORTANT! When regenerating the base client, remove any StringEnumConverter
    /// declarations on enum properties. 
    /// 
    /// This class allows a serialisation strategy to differ on reading responses, from sending.
    /// </summary>
    public class SampleRelyingParty : RelyingParty
    {
        //Seems to be down to daft FIDO lib serialisation approach
    
        StringEnumConverter readStrategy = new StringEnumConverter();
        FidoLibEnumConverter sendStrategy = new FidoLibEnumConverter();

        public SampleRelyingParty(HttpClient httpClient) : base(httpClient)
        {
            JsonSerializerSettings.Converters.Add(new Base64UrlConverter());
            JsonSerializerSettings.Converters.Add(sendStrategy);

            JsonSerializerSettings.Error = (sender, args) =>
            {
                if (System.Diagnostics.Debugger.IsAttached)
                {
                    System.Diagnostics.Debugger.Break();
                }
            };




        }

        protected override async Task<ObjectResponseResult<T>> ReadObjectResponseAsync<T>(HttpResponseMessage response, IReadOnlyDictionary<string, IEnumerable<string>> headers)
        {
            //need to change the serialisation strategy for when reading responses
          
            //nasty probably not thread safe hack
            JsonSerializerSettings.Converters.Add(readStrategy);
            JsonSerializerSettings.Converters.Remove(sendStrategy);

            var res= await base.ReadObjectResponseAsync<T>(response, headers);

            JsonSerializerSettings.Converters.Add(sendStrategy);
            JsonSerializerSettings.Converters.Remove(readStrategy);


            return res;
        }
    }
}
