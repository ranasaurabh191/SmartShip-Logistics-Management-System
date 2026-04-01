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
    private readonly IConfiguration _config;

    public ShipmentCreatedConsumer(INotificationService notification,
        IHttpClientFactory httpClientFactory, ILogger<ShipmentCreatedConsumer> logger, IConfiguration config)
    {
        _notification = notification;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _config = config;
    }

    public async Task Consume(ConsumeContext<ShipmentCreatedEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation("ShipmentCreatedEvent received | ShipmentId: {ShipmentId}", msg.ShipmentId);

        var email = await ConsumerHelper.GetUserEmailAsync(_httpClientFactory, _logger, msg.CustomerId, _config);
        if (email == null) return;

        var ist = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
        var createdAtIst = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(msg.CreatedAt, DateTimeKind.Utc), ist);

        await _notification.SendAndSaveAsync(
            msg.CustomerId, email,
            type: "ShipmentCreated",
            subject: $"Shipment Created — {msg.TrackingNumber}",
            body: $"""
            <h2>Your Shipment Has Been Created!</h2>
            <p><b>Tracking Number:</b> {msg.TrackingNumber}</p>
            <p><b>From:</b> {msg.SenderCity}</p>
            <p><b>Created At:</b> {createdAtIst:dd-MMM-yyyy hh:mm tt}</p>
            <br/>
            <p>Please complete payment and schedule pickup to proceed.</p>
            <p>— SmartShip Team</p>
        """
        );
    }
}