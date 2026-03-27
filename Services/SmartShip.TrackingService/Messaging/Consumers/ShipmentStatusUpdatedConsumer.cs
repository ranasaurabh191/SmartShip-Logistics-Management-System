using MassTransit;
using SmartShip.Shared.Events;
using SmartShip.TrackingService.Data;
using SmartShip.TrackingService.Models;

public class ShipmentStatusUpdatedConsumer : IConsumer<ShipmentStatusUpdatedEvent>
{
    private readonly TrackingDbContext _db;
    public ShipmentStatusUpdatedConsumer(TrackingDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<ShipmentStatusUpdatedEvent> context)
    {
        var msg = context.Message;

        _db.TrackingEvents.Add(new TrackingEvent
        {
            ShipmentId = msg.ShipmentId,
            TrackingNumber = msg.TrackingNumber,
            Status = msg.NewStatus,
            Location = msg.Location,
            Description = $"Status updated to {msg.NewStatus}",
            EventTime = msg.UpdatedAt,
            UpdatedBy = msg.UpdatedBy
        });

        await _db.SaveChangesAsync();
    }
}