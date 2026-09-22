using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace WardrobeApi.Services;

public interface IEmailService
{
    Task SendConfirmationEmailAsync(string toEmail, string confirmLink);
}

// Uses Brevo's transactional email HTTP API (https://api.brevo.com) instead
// of SMTP. Render's free tier blocks outbound SMTP ports (25/465/587), but
// this goes over plain HTTPS (port 443), so it works there without needing
// a paid instance.
public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly HttpClient _httpClient;

    public EmailService(IConfiguration config, HttpClient httpClient)
    {
        _config = config;
        _httpClient = httpClient;
    }

    public async Task SendConfirmationEmailAsync(string toEmail, string confirmLink)
    {
        var senderName = _config["EmailSettings:SenderName"] ?? "WardrobeIQ";
        var senderEmail = _config["EmailSettings:SenderEmail"];
        var apiKey = _config["EmailSettings:BrevoApiKey"];

        var htmlContent = $@"
            <div style='font-family: sans-serif; max-width: 480px; margin: 0 auto;'>
                <h2 style='color: #D4AF6A;'>Welcome to WardrobeIQ</h2>
                <p>Tap the button below to confirm your email and start using the app.</p>
                <a href='{confirmLink}'
                   style='display: inline-block; padding: 12px 24px; background: #D4AF6A;
                          color: #1A1418; text-decoration: none; border-radius: 8px; font-weight: bold;'>
                    Confirm Email
                </a>
                <p style='color: #888; font-size: 13px; margin-top: 24px;'>
                    If the button doesn't work, copy and paste this link into your browser: {confirmLink}
                </p>
            </div>";

        var payload = new
        {
            sender = new { name = senderName, email = senderEmail },
            to = new[] { new { email = toEmail } },
            subject = "Confirm your WardrobeIQ account",
            htmlContent
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        request.Headers.TryAddWithoutValidation("api-key", apiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new Exception($"Brevo API returned {(int)response.StatusCode}: {body}");
        }
    }
}