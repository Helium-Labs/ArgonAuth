using GameServer;

namespace SampleClient.Data
{

    //DEMO Connection with hardwired values.
    public class RelyingPartyConnection
    {
        public RelyingParty RelyingParty { private set; get; }

        public RelyingPartyConnection()
        {
            HttpClient httpClient = new HttpClient();

            RelyingParty= new RelyingParty("https://localhost:5001",httpClient);
        }
    }
}
