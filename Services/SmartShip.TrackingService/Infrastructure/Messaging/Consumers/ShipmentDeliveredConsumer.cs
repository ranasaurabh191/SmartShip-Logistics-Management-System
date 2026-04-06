using MassTransit;
using SmartShip.Shared.Events;
using SmartShip.TrackingService.Domain.Entities;
using SmartShip.TrackingService.Infrastructure.Data;

namespace SmartShip.TrackingService.Infrastructure.Messaging.Consumers;
public class ShipmentDeliveredConsumer : IConsumer<ShipmentDeliveredEvent>
{
    private readonly TrackingDbContext _db;
    private readonly ILogger<ShipmentDeliveredConsumer> _logger;

    public ShipmentDeliveredConsumer(
        TrackingDbContext db,
        ILogger<ShipmentDeliveredConsumer> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ShipmentDeliveredEvent> context)
    {
        var msg = context.Message;

        _logger.LogInformation("Shipment Delivered Tracking Event Updated.");

        _db.TrackingEvents.Add(new TrackingEvent
        {
            ShipmentId = msg.ShipmentId,
            TrackingNumber = msg.TrackingNumber,
            Status = "Delivered",
            Location = msg.Location ?? "Customers Address",
            Description = "Shipment created",
            EventTime = msg.DeliveredAt,
            UpdatedBy = "system"
        });

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Created Shipment Delivered event for {TrackingNumber}",
            msg.TrackingNumber);
    }
}
