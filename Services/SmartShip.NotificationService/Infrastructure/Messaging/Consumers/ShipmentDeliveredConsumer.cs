using MassTransit;
using SmartShip.NotificationService.Core.Interfaces.Services;
using SmartShip.NotificationService.Infrastructure.Helpers;
using SmartShip.Shared.Events;

namespace SmartShip.NotificationService.Infrastructure.Messaging.Consumers;

public class ShipmentDeliveredConsumer : IConsumer<ShipmentDeliveredEvent>
{
    private readonly INotificationService _notification;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ShipmentDeliveredConsumer> _logger;
    private readonly IConfiguration _config;

    public ShipmentDeliveredConsumer(
        INotificationService notification,
        IHttpClientFactory httpClientFactory,
        ILogger<ShipmentDeliveredConsumer> logger,
        IConfiguration config)
    {
        _notification = notification;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _config = config;
    }

    public async Task Consume(ConsumeContext<ShipmentDeliveredEvent> context)
    {
        var msg = context.Message;

        _logger.LogInformation(
            "ShipmentDeliveredEvent received | Tracking: {TrackingNumber}",
            msg.TrackingNumber);

        var email = await ConsumerHelper.GetUserEmailAsync(
            _httpClientFactory,
            _logger,
            msg.CustomerId,
            _config);

        if (email == null) return;

        await _notification.SendAndSaveAsync(
            msg.CustomerId,
            email,
            type: "ShipmentDelivered",
            subject: $"Shipment Delivered — {msg.TrackingNumber}",
            body: EmailTemplates.ShipmentDelivered(
                msg.TrackingNumber,
                msg.DeliveredAt.ToString("dd-MMM-yyyy hh:mm tt"))
        );
    }
}