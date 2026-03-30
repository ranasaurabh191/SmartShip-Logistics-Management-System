using MassTransit;
using SmartShip.NotificationService.Helpers;
using SmartShip.NotificationService.Services;
using SmartShip.Shared.Events;

namespace SmartShip.NotificationService.Messaging.Consumers;

public class ShipmentDeliveredConsumer : IConsumer<ShipmentDeliveredEvent>
{
    private readonly INotificationService _notification;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ShipmentDeliveredConsumer> _logger;

    public ShipmentDeliveredConsumer(INotificationService notification,
        IHttpClientFactory httpClientFactory, ILogger<ShipmentDeliveredConsumer> logger)
    {
        _notification = notification;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ShipmentDeliveredEvent> context)
    {
        var e = context.Message;
        _logger.LogInformation("ShipmentDeliveredEvent received | Tracking: {TrackingNumber}", e.TrackingNumber);

        var email = await ConsumerHelper.GetUserEmailAsync(_httpClientFactory, _logger, e.CustomerId);
        if (email == null) return;

        await _notification.SendAndSaveAsync(
            e.CustomerId, email,
            type: "ShipmentDelivered",
            subject: $"Shipment Delivered — {e.TrackingNumber} ",
            body: $"""
            <h2>Your Shipment Has Been Delivered!</h2>
            <p><b>Tracking Number:</b> {e.TrackingNumber}</p>
            <p><b>Delivered At:</b> {e.DeliveredAt:dd-MMM-yyyy hh:mm tt}</p>
            <br/>
            <p>Thank you for using SmartShip!</p>
            <p>— SmartShip Team</p>
        """
        );
    }
}

