using MassTransit;
using SmartShip.Shared.Events;
using SmartShip.ShipmentService.Infrastructure.Data;
using SmartShip.ShipmentService.Domain.Enums;

namespace SmartShip.ShipmentService.Infrastructure.Messaging.Consumers;

public class CancelShipmentConsumer : IConsumer<CancelShipmentCommand>
{
    private readonly ShipmentDbContext _context;
    private readonly ILogger<CancelShipmentConsumer> _logger;
    private readonly IPublishEndpoint _publisher;

    public CancelShipmentConsumer(ShipmentDbContext context,
        ILogger<CancelShipmentConsumer> logger, IPublishEndpoint publisher)
    {
        _context = context;
        _logger = logger;
        _publisher = publisher;
    }

    public async Task Consume(ConsumeContext<CancelShipmentCommand> context)
    {
        var msg = context.Message;
        _logger.LogInformation("CancelShipmentCommand received | ShipmentId: {ShipmentId} | Reason: {Reason}",
            msg.ShipmentId, msg.Reason);

        var shipment = await _context.Shipments.FindAsync(msg.ShipmentId);

        if (shipment == null)
        {
            _logger.LogWarning("Shipment {ShipmentId} not found for cancellation.", msg.ShipmentId);
            return;
        }

        if (shipment.Status != ShipmentStatus.Draft)
        {
            _logger.LogWarning("Shipment {ShipmentId} is in {Status} — skipping cancellation.",
                msg.ShipmentId, shipment.Status);
            return;
        }

        shipment.Status = ShipmentStatus.Cancelled;
        shipment.Notes = $"Auto-cancelled: {msg.Reason}";
        await _context.SaveChangesAsync();

        _logger.LogInformation("Shipment {TrackingNumber} auto-cancelled due to payment failure.",
            msg.TrackingNumber);

        await _publisher.Publish(new ShipmentCancelledEvent
        {
            ShipmentId = shipment.Id,
            TrackingNumber = shipment.TrackingNumber,
            CustomerId = shipment.CustomerId,
            CancelledAt = DateTime.UtcNow
        });
    }
}