using MassTransit;
using SmartShip.NotificationService.Services;
using SmartShip.Shared.Events;
using SmartShip.NotificationService.Helpers;

namespace SmartShip.NotificationService.Messaging.Consumers;

public class PaymentCompletedConsumer : IConsumer<PaymentCompletedEvent>
{
    private readonly INotificationService _notification;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PaymentCompletedConsumer> _logger;

    public PaymentCompletedConsumer(INotificationService notification,
        IHttpClientFactory httpClientFactory, ILogger<PaymentCompletedConsumer> logger)
    {
        _notification = notification;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PaymentCompletedEvent> context)
    {
        var e = context.Message;
        _logger.LogInformation("PaymentCompletedEvent received | Tracking: {TrackingNumber}", e.TrackingNumber);

        var customerId = await ConsumerHelper.GetCustomerIdAsync(_httpClientFactory, _logger, e.ShipmentId);
        if (customerId == null) return;

        var email = await ConsumerHelper.GetUserEmailAsync(_httpClientFactory, _logger, customerId.Value);
        if (email == null) return;

        await _notification.SendAndSaveAsync(
            customerId.Value, email,
            type: "PaymentCompleted",
            subject: $"Payment Confirmed — {e.TrackingNumber}",
            body: $"""
            <h2>Payment Confirmed!</h2>
            <p><b>Tracking Number:</b> {e.TrackingNumber}</p>
            <p><b>Method:</b> {e.PaymentMethod}</p>
            <p><b>Status:</b> {e.PaymentStatus}</p>
            <br/>
            <p>You can now schedule your pickup.</p>
            <p>— SmartShip Team</p>
        """
        );
    }
}
