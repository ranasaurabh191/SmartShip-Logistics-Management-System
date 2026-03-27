using MassTransit;
using SmartShip.Shared.Events;
using SmartShip.TrackingService.Data;
using SmartShip.TrackingService.Models;

public class ShipmentStatusUpdatedConsumer : IConsumer<ShipmentStatusUpdatedEvent>
{
    private readonly TrackingDbContext _db;
    private readonly ILogger<ShipmentStatusUpdatedConsumer> _logger;

    public ShipmentStatusUpdatedConsumer(TrackingDbContext db, ILogger<ShipmentStatusUpdatedConsumer> logger)
    {
        _db = db;
        _logger = logger;
    }
    public async Task Consume(ConsumeContext<ShipmentStatusUpdatedEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation("Processing StatusUpdate for Tracking Number: {TrackingNumber} {OldStatus} -> {NewStatus}",
            msg.TrackingNumber, msg.OldStatus, msg.NewStatus);

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
        _logger.LogInformation("Changes Saved, Added status event for Tracking Number: {TrackingNumber}", msg.TrackingNumber);
    }
}