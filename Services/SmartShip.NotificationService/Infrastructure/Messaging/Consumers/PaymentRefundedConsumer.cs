using MassTransit;
using SmartShip.NotificationService.Core.Interfaces.Services;
using SmartShip.NotificationService.Infrastructure.Helpers;
using SmartShip.Shared.Events;

namespace SmartShip.NotificationService.Infrastructure.Messaging.Consumers;

public class PaymentRefundedConsumer : IConsumer<PaymentRefundedEvent>
{
    private readonly IEmailService _emailService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PaymentRefundedConsumer> _logger;
    private readonly IConfiguration _config;

    public PaymentRefundedConsumer(
        IEmailService emailService,
        IHttpClientFactory httpClientFactory,
        ILogger<PaymentRefundedConsumer> logger,
        IConfiguration config)
    {
        _emailService = emailService;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _config = config;
    }

    public async Task Consume(ConsumeContext<PaymentRefundedEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation("PaymentRefundedEvent received | Tracking: {TrackingNumber} | Amount: {Amount}",
            msg.TrackingNumber, msg.Amount);

        var email = await ConsumerHelper.GetUserEmailAsync(_httpClientFactory, _logger, msg.CustomerId, _config);

        if (string.IsNullOrEmpty(email)) return;

        _logger.LogInformation("Sending notification | Type: PaymentRefunded | User: {UserId} | Email: {Email}",
            msg.CustomerId, email);

        await _emailService.SendEmailAsync(
            email,
            $"Refund Processed — {msg.TrackingNumber}",
            EmailTemplates.PaymentRefunded(
                msg.TrackingNumber,
                msg.Amount.ToString("F2"),
                msg.RefundedAt.ToLocalTime().ToString("dd-MMM-yyyy hh:mm tt")));

        _logger.LogInformation("Refund email sent to {Email}", email);
    }
}