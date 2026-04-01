using MassTransit;
using Microsoft.VisualBasic;
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
    private readonly IConfiguration _config;

    public ShipmentCancelledConsumer(INotificationService notification,
        IHttpClientFactory httpClientFactory, ILogger<ShipmentCancelledConsumer> logger, IConfiguration config)
    {
        _notification = notification;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _config = config;
        _config = config;
    }

    public async Task Consume(ConsumeContext<ShipmentCancelledEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation("ShipmentCancelledEvent received | Tracking: {TrackingNumber}", msg.TrackingNumber);

        var customerId = msg.CustomerId;

        var email = await ConsumerHelper.GetUserEmailAsync(_httpClientFactory, _logger, customerId, _config);
        if (email == null) return;

        var ist = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
        var cancelledAtIst = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(msg.CancelledAt, DateTimeKind.Utc), ist);

        await _notification.SendAndSaveAsync(
            customerId, email,
            type: "ShipmentCancelled",
            subject: $"Shipment Cancelled — {msg.TrackingNumber}",
            body: $"""
            <h2>Your Shipment Has Been Cancelled</h2>
            <p><b>Tracking Number:</b> {msg.TrackingNumber}</p>
            <p><b>Cancelled At:</b> {cancelledAtIst:dd-MMM-yyyy hh:mm tt}</p>
            <br/>
            <p>If this was a mistake, please contact support.</p>
            <p>— SmartShip Team</p>
        """
        );
    }
}
