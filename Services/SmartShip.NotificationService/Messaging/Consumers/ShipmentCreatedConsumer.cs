using MassTransit;
using SmartShip.NotificationService.Helpers;
using SmartShip.NotificationService.Services;
using SmartShip.Shared.Events;

namespace SmartShip.NotificationService.Consumers;

public class ShipmentCreatedConsumer : IConsumer<ShipmentCreatedEvent>
{
    private readonly INotificationService _notification;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ShipmentCreatedConsumer> _logger;

    public ShipmentCreatedConsumer(INotificationService notification,
        IHttpClientFactory httpClientFactory, ILogger<ShipmentCreatedConsumer> logger)
    {
        _notification = notification;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ShipmentCreatedEvent> context)
    {
        var e = context.Message;
        _logger.LogInformation("ShipmentCreatedEvent received | ShipmentId: {ShipmentId}", e.ShipmentId);

        var email = await ConsumerHelper.GetUserEmailAsync(_httpClientFactory, _logger, e.CustomerId);
        if (email == null) return;

        await _notification.SendAndSaveAsync(
            e.CustomerId, email,
            type: "ShipmentCreated",
            subject: $"Shipment Created — {e.TrackingNumber}",
            body: $"""
                <h2>Your Shipment Has Been Created!</h2>
                <p><b>Tracking Number:</b> {e.TrackingNumber}</p>
                <p><b>From:</b> {e.SenderCity}</p>
                <p><b>Created At:</b> {e.CreatedAt:dd-MMM-yyyy hh:mm tt}</p>
                <br/>
                <p>Please complete payment and schedule pickup to proceed.</p>
                <p>— SmartShip Team</p>
            """
        );
    }
}