using MassTransit;
using SmartShip.NotificationService.Core.Interfaces.Services;
using SmartShip.NotificationService.Infrastructure.Helpers;
using SmartShip.Shared.Events;

namespace SmartShip.NotificationService.Infrastructure.Messaging.Consumers;

public class PaymentCompletedConsumer : IConsumer<PaymentCompletedEvent>
{
    private readonly INotificationService _notification;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PaymentCompletedConsumer> _logger;
    private readonly IConfiguration _config;

    public PaymentCompletedConsumer(INotificationService notification,
        IHttpClientFactory httpClientFactory, ILogger<PaymentCompletedConsumer> logger, IConfiguration config)
    {
        _notification = notification;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _config = config;
    }

    public async Task Consume(ConsumeContext<PaymentCompletedEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation("PaymentCompletedEvent received | Tracking: {TrackingNumber}", msg.TrackingNumber);

        var customerId = msg.CustomerId;

        var email = await ConsumerHelper.GetUserEmailAsync(_httpClientFactory, _logger, customerId, _config);
        if (email == null) return;

        await _notification.SendAndSaveAsync(
            customerId, email,
            type: "PaymentCompleted",
            subject: $"Payment Confirmed — {msg.TrackingNumber}",
            body: EmailTemplates.PaymentCompleted(
                msg.TrackingNumber,
                msg.ShipmentId,
                msg.PaymentMethod,
                msg.PaymentStatus,
                msg.Amount,
                msg.PaidAt,
                msg.RazorpayPaymentId,
                msg.RazorpayOrderId)
        );
    }
}
