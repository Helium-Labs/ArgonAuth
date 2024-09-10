using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

public interface IEmailService
{
    Task<bool> SendEmailAsync(string to, string subject, string htmlBody);
}

public class EmailService : IEmailService
{
    private readonly HttpClient _httpClient;
    private readonly string _bearerToken = "re_f8X6aqoa_LZCVSDFe1ofKjeAbG5dSEU1k"; // Replace with your actual bearer token

    public EmailService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> SendEmailAsync(string to, string subject, string htmlBody)
    {
        var emailRequest = new
        {
            from = "\"Onboarding\" <noreply@volera.ai>",
            to = to,
            subject = subject,
            html = htmlBody
        };

        var jsonRequest = JsonConvert.SerializeObject(emailRequest);
        var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _bearerToken);

        var response = await _httpClient.PostAsync("https://api.resend.com/emails", content);
        return response.IsSuccessStatusCode;
    }
}