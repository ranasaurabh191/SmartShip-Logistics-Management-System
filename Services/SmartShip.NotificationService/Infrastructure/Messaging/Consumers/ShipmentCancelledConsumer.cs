using MassTransit;
using SmartShip.NotificationService.Core.Interfaces.Services;
using SmartShip.NotificationService.Infrastructure.Helpers;
using SmartShip.Shared.Events;

namespace SmartShip.NotificationService.Infrastructure.Messaging.Consumers;

public class ShipmentCancelledConsumer : IConsumer<ShipmentCancelledEvent>
{
    private readonly INotificationService _notification;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ShipmentCancelledConsumer> _logger;
    private readonly IConfiguration _config;

    public ShipmentCancelledConsumer(
        INotificationService notification,
        IHttpClientFactory httpClientFactory,
        ILogger<ShipmentCancelledConsumer> logger,
        IConfiguration config)
    {
        _notification = notification;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _config = config;
    }

    public async Task Consume(ConsumeContext<ShipmentCancelledEvent> context)
    {
        var msg = context.Message;

        _logger.LogInformation(
            "ShipmentCancelledEvent received | Tracking: {TrackingNumber}",
            msg.TrackingNumber);

        var customerId = msg.CustomerId;

        var email = await ConsumerHelper.GetUserEmailAsync(
            _httpClientFactory, _logger, customerId, _config);

        if (email == null) return;

        await _notification.SendAndSaveAsync(
            customerId,
            email,
            type: "ShipmentCancelled",
            subject: $"Shipment Cancelled — {msg.TrackingNumber}",
            body: EmailTemplates.ShipmentCancelled(
                msg.TrackingNumber,
                msg.CancelledAt.ToString("dd-MMM-yyyy hh:mm tt"))
        );
    }
}