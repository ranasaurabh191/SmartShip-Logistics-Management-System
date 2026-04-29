using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using SmartShip.NotificationService.Core.Interfaces.Services;
using SmartShip.NotificationService.Infrastructure.Helpers;

namespace SmartShip.NotificationService.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogInformation("=== EmailService Config Debug ===");
        _logger.LogInformation("EmailSettings exists: {Exists}", _config.GetSection("EmailSettings").Exists());
        _logger.LogInformation("SenderEmail: '{Sender}'", _config["EmailSettings:SenderEmail"] ?? "NULL ");
        _logger.LogInformation("Host: '{Host}'", _config["EmailSettings:Host"] ?? "NULL ");
        _logger.LogInformation("Port: '{Port}'", _config["EmailSettings:Port"] ?? "NULL ");
        _logger.LogInformation("===============================");
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
            throw new ArgumentException("Recipient email cannot be null or empty");

        var settings = _config.GetSection("EmailSettings");

        var senderEmail = settings["SenderEmail"];
        var senderName = settings["SenderName"] ?? "SmartShip";
        var host = settings["Host"];
        var port = int.Parse(settings["Port"] ?? "587");
        var password = settings["Password"];

        if (string.IsNullOrWhiteSpace(senderEmail))
            throw new InvalidOperationException("SenderEmail missing from EmailSettings");

        if (string.IsNullOrWhiteSpace(host))
            throw new InvalidOperationException("EmailSettings:Host missing");

        if (string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("EmailSettings:Password missing");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(senderName, senderEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = body };

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(host, port, SecureSocketOptions.StartTls);
        await smtp.AuthenticateAsync(senderEmail, password);
        await smtp.SendAsync(message);
        await smtp.DisconnectAsync(true);

        _logger.LogInformation("Email sent to {Email}", toEmail);
    }

    public async Task SendOtpEmailAsync(string toEmail, string otp)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
            throw new ArgumentException("Email address cannot be null or empty");

        await SendEmailAsync(toEmail, "SmartShip OTP Verification", EmailTemplates.OtpVerification(otp));
    }
}