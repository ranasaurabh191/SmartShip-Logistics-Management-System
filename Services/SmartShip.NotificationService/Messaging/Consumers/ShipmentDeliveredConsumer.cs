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
    private readonly IConfiguration _config;

    public ShipmentDeliveredConsumer(INotificationService notification,
        IHttpClientFactory httpClientFactory, ILogger<ShipmentDeliveredConsumer> logger, IConfiguration config)
    {
        _notification = notification;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _config = config;
    }

    public async Task Consume(ConsumeContext<ShipmentDeliveredEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation("ShipmentDeliveredEvent received | Tracking: {TrackingNumber}", msg.TrackingNumber);

        var email = await ConsumerHelper.GetUserEmailAsync(_httpClientFactory, _logger, msg.CustomerId, _config);
        if (email == null) return;

        var ist = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
        var deliveredAtIst = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(msg.DeliveredAt, DateTimeKind.Utc), ist);

        await _notification.SendAndSaveAsync(
            msg.CustomerId, email,
            type: "ShipmentDelivered",
            subject: $"Shipment Delivered — {msg.TrackingNumber} ",
            body: $"""
            <h2>Your Shipment Has Been Delivered!</h2>
            <p><b>Tracking Number:</b> {msg.TrackingNumber}</p>
            <p><b>Delivered At:</b> {deliveredAtIst:dd-MMM-yyyy hh:mm tt}</p>
            <br/>
            <p>Thank you for using SmartShip!</p>
            <p>— SmartShip Team</p>
        """
        );
    }
}

