using GameServer;
using Newtonsoft.Json.Converters;

namespace SampleClient.Data
{

    //DEMO Connection with hardwired values.
    public class RelyingPartyConnection
    {
        public SampleRelyingParty RelyingParty { private set; get; }

        public RelyingPartyConnection()
        {
            HttpClient httpClient = new HttpClient();

            RelyingParty= new SampleRelyingParty(httpClient);
       
            RelyingParty.BaseUrl = "https://localhost:5001";
        }
    }
}
