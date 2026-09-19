using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace WardrobeApi.Services;

public interface IEmailService
{
    Task SendConfirmationEmailAsync(string toEmail, string confirmLink);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendConfirmationEmailAsync(string toEmail, string confirmLink)
    {
        var senderName = _config["EmailSettings:SenderName"] ?? "WardrobeIQ";
        var senderEmail = _config["EmailSettings:SenderEmail"];
        var appPassword = _config["EmailSettings:Password"];
        var smtpHost = _config["EmailSettings:Server"] ?? "smtp.gmail.com";
        var smtpPort = int.Parse(_config["EmailSettings:Port"] ?? "587");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(senderName, senderEmail));
        message.To.Add(new MailboxAddress("", toEmail));
        message.Subject = "Confirm your WardrobeIQ account";

        message.Body = new TextPart("html")
        {
            Text = $@"
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
                </div>"
        };

        using var client = new MailKit.Net.Smtp.SmtpClient();
        await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(senderEmail, appPassword);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}