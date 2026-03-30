using MassTransit;
using SmartShip.Shared.Events;

namespace SmartShip.PaymentService.Messaging.Consumers;

public class ShipmentCreatedConsumer : IConsumer<ShipmentCreatedEvent>
{
    private readonly ILogger<ShipmentCreatedConsumer> _logger;

    public ShipmentCreatedConsumer(ILogger<ShipmentCreatedConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<ShipmentCreatedEvent> context)
    {
        _logger.LogInformation("Payment notified: Shipment {ShipmentId} | Tracking: {TrackingNumber} | Customer: {CustomerId}",
            context.Message.ShipmentId,
            context.Message.TrackingNumber,
            context.Message.CustomerId);

        return Task.CompletedTask;
    }
}