using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace SmartShip.NotificationService.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        var settings = _config.GetSection("EmailSettings");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(
            settings["SenderName"],
            settings["SenderEmail"]!));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = body };

        using var smtp = new SmtpClient();

        await smtp.ConnectAsync(
            settings["Host"],
            int.Parse(settings["Port"]!),
            SecureSocketOptions.StartTls);

        await smtp.AuthenticateAsync(
            settings["SenderEmail"],
            settings["Password"]);

        await smtp.SendAsync(message);
        await smtp.DisconnectAsync(true);

        _logger.LogInformation("Email sent to {Email} | Subject: {Subject}", toEmail, subject);
    }
}