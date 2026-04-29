using MassTransit;
using SmartShip.NotificationService.Core.Interfaces.Services;
using SmartShip.NotificationService.Infrastructure.Helpers;
using SmartShip.Shared.Events;

namespace SmartShip.NotificationService.Infrastructure.Messaging.Consumers;

public class ShipmentStatusUpdatedConsumer : IConsumer<ShipmentStatusUpdatedEvent>
{
    private readonly INotificationService _notification;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ShipmentStatusUpdatedConsumer> _logger;
    private readonly IConfiguration _config;

    public ShipmentStatusUpdatedConsumer(
        INotificationService notification,
        IHttpClientFactory httpClientFactory,
        ILogger<ShipmentStatusUpdatedConsumer> logger,
        IConfiguration config)
    {
        _notification = notification;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _config = config;
    }

    public async Task Consume(ConsumeContext<ShipmentStatusUpdatedEvent> context)
    {
        var msg = context.Message;

        _logger.LogInformation(
            "ShipmentStatusUpdatedEvent received | Tracking: {TrackingNumber}",
            msg.TrackingNumber);

        var customerId = msg.CustomerId;

        var email = await ConsumerHelper.GetUserEmailAsync(
            _httpClientFactory,
            _logger,
            customerId,
            _config);

        if (email == null) return;

        await _notification.SendAndSaveAsync(
            customerId,
            email,
            type: "StatusUpdated",
            subject: $"Shipment Update — {msg.TrackingNumber}",
            body: EmailTemplates.ShipmentStatusUpdated(
                msg.TrackingNumber,
                msg.OldStatus,
                msg.NewStatus,
                msg.Location,
                msg.UpdatedAt.ToString("dd-MMM-yyyy hh:mm tt"))
        );
    }
}