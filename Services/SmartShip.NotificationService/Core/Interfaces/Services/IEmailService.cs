namespace SmartShip.NotificationService.Core.Interfaces.Services;

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string body);
    Task SendOtpEmailAsync(string toEmail, string otp);
}