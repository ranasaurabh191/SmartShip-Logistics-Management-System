using MassTransit;
using SmartShip.NotificationService.Helpers;
using SmartShip.NotificationService.Services;
using SmartShip.Shared.Events;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace SmartShip.NotificationService.Messaging.Consumers;

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
            body: $"""
            <h2>Payment Confirmed!</h2>
            <h2>Your Shipment has been booked.</h2>
            <p><b>Tracking Number:</b> {msg.TrackingNumber}</p>
            <p><b>Method:</b> {msg.PaymentMethod}</p>
            <p><b>Status:</b> {msg.PaymentStatus}</p>
            <br/>
            <p>You can now schedule your pickup.</p>
            <p>— SmartShip Team</p>
        """
        );
    }
}
