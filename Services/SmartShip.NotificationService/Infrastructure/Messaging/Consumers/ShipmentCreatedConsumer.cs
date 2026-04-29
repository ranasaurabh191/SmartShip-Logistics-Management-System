using MassTransit;
using SmartShip.NotificationService.Core.Interfaces.Services;
using SmartShip.NotificationService.Infrastructure.Helpers;
using SmartShip.Shared.Events;

namespace SmartShip.NotificationService.Infrastructure.Messaging.Consumers;

public class ShipmentCreatedConsumer : IConsumer<ShipmentCreatedEvent>
{
    private readonly INotificationService _notification;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ShipmentCreatedConsumer> _logger;
    private readonly IConfiguration _config;

    public ShipmentCreatedConsumer(
        INotificationService notification,
        IHttpClientFactory httpClientFactory,
        ILogger<ShipmentCreatedConsumer> logger,
        IConfiguration config)
    {
        _notification = notification;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _config = config;
    }

    public async Task Consume(ConsumeContext<ShipmentCreatedEvent> context)
    {
        var msg = context.Message;

        _logger.LogInformation(
            "ShipmentCreatedEvent received | ShipmentId: {ShipmentId}",
            msg.ShipmentId);

        var email = await ConsumerHelper.GetUserEmailAsync(
            _httpClientFactory,
            _logger,
            msg.CustomerId,
            _config);

        if (email == null) return;

        await _notification.SendAndSaveAsync(
            msg.CustomerId,
            email,
            type: "ShipmentCreated",
            subject: $"Shipment Created — {msg.TrackingNumber}",
            body: EmailTemplates.ShipmentCreated(
                msg.TrackingNumber,
                msg.SenderCity,
                msg.CreatedAt.ToString("dd-MMM-yyyy hh:mm tt"))
        );
    }
}