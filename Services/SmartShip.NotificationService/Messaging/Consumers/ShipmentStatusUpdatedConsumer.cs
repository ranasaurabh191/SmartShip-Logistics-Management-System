using MassTransit;
using SmartShip.NotificationService.Helpers;
using SmartShip.NotificationService.Services;
using SmartShip.Shared.Events;

namespace SmartShip.NotificationService.Messaging.Consumers;

public class ShipmentStatusUpdatedConsumer : IConsumer<ShipmentStatusUpdatedEvent>
{
    private readonly INotificationService _notification;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ShipmentStatusUpdatedConsumer> _logger;

    public ShipmentStatusUpdatedConsumer(INotificationService notification,
        IHttpClientFactory httpClientFactory, ILogger<ShipmentStatusUpdatedConsumer> logger)
    {
        _notification = notification;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ShipmentStatusUpdatedEvent> context)
    {
        var e = context.Message;
        _logger.LogInformation("ShipmentStatusUpdatedEvent received | Tracking: {TrackingNumber}", e.TrackingNumber);

        var customerId = await ConsumerHelper.GetCustomerIdAsync(_httpClientFactory, _logger, e.ShipmentId);
        if (customerId == null) return;

        var email = await ConsumerHelper.GetUserEmailAsync(_httpClientFactory, _logger, customerId.Value);
        if (email == null) return;

        await _notification.SendAndSaveAsync(
            customerId.Value, email,
            type: "StatusUpdated",
            subject: $"Shipment Update — {e.TrackingNumber}",
            body: $"""
            <h2>Your Shipment Status Has Changed!</h2>
            <p><b>Tracking Number:</b> {e.TrackingNumber}</p>
            <p><b>Status:</b> {e.OldStatus} -> <b>{e.NewStatus}</b></p>
            <p><b>Location:</b> {e.Location}</p>
            <p><b>Updated At:</b> {e.UpdatedAt:dd-MMM-yyyy hh:mm tt}</p>
            <p>— SmartShip Team</p>
        """
        );
    }
}

