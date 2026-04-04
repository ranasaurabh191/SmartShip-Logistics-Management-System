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

        await _emailService.SendEmailAsync(email, "Refund Processed — SmartShip",
            $"""
            <h2>Refund Processed</h2>
            <p>Your refund has been processed for the following shipment:</p>
            <ul>
                <li><b>Tracking Number:</b> {msg.TrackingNumber}</li>
                <li><b>Refund Amount:</b> ₹{msg.Amount:F2}</li>
                <li><b>Refunded At:</b> {msg.RefundedAt.ToLocalTime():dd-MMM-yyyy hh:mm tt}</li>
            </ul>
            <p>The refund will reflect in your account within 5-7 business days.</p>
            <p>Thank you for using SmartShip.</p>
            """);

        _logger.LogInformation("Refund email sent to {Email}", email);
    }
}