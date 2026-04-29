using MassTransit;
using SmartShip.NotificationService.Core.Interfaces.Services;
using SmartShip.NotificationService.Infrastructure.Helpers;
using SmartShip.Shared.Events;

namespace SmartShip.NotificationService.Infrastructure.Messaging.Consumers;

public class PaymentFailedConsumer : IConsumer<PaymentFailedEvent>
{
    private readonly INotificationService _notification;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PaymentFailedConsumer> _logger;
    private readonly IConfiguration _config;

    public PaymentFailedConsumer(
        INotificationService notification,
        IHttpClientFactory httpClientFactory,
        ILogger<PaymentFailedConsumer> logger,
        IConfiguration config)
    {
        _notification = notification;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _config = config;
    }

    public async Task Consume(ConsumeContext<PaymentFailedEvent> context)
    {
        var msg = context.Message;

        _logger.LogInformation(
            "PaymentFailedEvent received | Tracking: {TrackingNumber}",
            msg.TrackingNumber);

        var customerId = msg.CustomerId;

        var email = await ConsumerHelper.GetUserEmailAsync(
            _httpClientFactory,
            _logger,
            customerId,
            _config);

        if (email == null) return;

        await _notification.SendAndSaveAsync(
            customerId,
            email,
            type: "PaymentFailed",
            subject: $"Payment Failed — {msg.TrackingNumber}",
            body: EmailTemplates.PaymentFailed(
                msg.TrackingNumber,
                msg.FailedAt.ToString("dd-MMM-yyyy hh:mm tt"))
        );
    }
}