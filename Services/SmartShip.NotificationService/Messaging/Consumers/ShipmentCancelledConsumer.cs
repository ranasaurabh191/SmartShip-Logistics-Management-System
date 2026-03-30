using MassTransit;
using SmartShip.NotificationService.DTOs;
using SmartShip.NotificationService.Helpers;
using SmartShip.NotificationService.Services;
using SmartShip.Shared.Events;

namespace SmartShip.NotificationService.Messaging.Consumers;

public class ShipmentCancelledConsumer : IConsumer<ShipmentCancelledEvent>
{
    private readonly INotificationService _notification;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ShipmentCancelledConsumer> _logger;

    public ShipmentCancelledConsumer(INotificationService notification,
        IHttpClientFactory httpClientFactory, ILogger<ShipmentCancelledConsumer> logger)
    {
        _notification = notification;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ShipmentCancelledEvent> context)
    {
        var e = context.Message;
        _logger.LogInformation("ShipmentCancelledEvent received | Tracking: {TrackingNumber}", e.TrackingNumber);

        var customerId = await ConsumerHelper.GetCustomerIdAsync(_httpClientFactory, _logger, e.ShipmentId);
        if (customerId == null) return;

        var email = await ConsumerHelper.GetUserEmailAsync(_httpClientFactory, _logger, customerId.Value);
        if (email == null) return;

        await _notification.SendAndSaveAsync(
            customerId.Value, email,
            type: "ShipmentCancelled",
            subject: $"Shipment Cancelled — {e.TrackingNumber}",
            body: $"""
            <h2>Your Shipment Has Been Cancelled</h2>
            <p><b>Tracking Number:</b> {e.TrackingNumber}</p>
            <p><b>Cancelled At:</b> {e.CancelledAt:dd-MMM-yyyy hh:mm tt}</p>
            <br/>
            <p>If this was a mistake, please contact support.</p>
            <p>— SmartShip Team</p>
        """
        );
    }
}
