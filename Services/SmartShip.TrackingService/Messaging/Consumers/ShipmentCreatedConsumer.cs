using MassTransit;
using SmartShip.Shared.Events;
using SmartShip.TrackingService.Models;
using SmartShip.TrackingService.Data;

public class ShipmentCreatedConsumer : IConsumer<ShipmentCreatedEvent>
{
    private readonly TrackingDbContext _db;
    private readonly ILogger<ShipmentCreatedConsumer> _logger;

    public ShipmentCreatedConsumer(TrackingDbContext db, ILogger<ShipmentCreatedConsumer> logger)
    {
        _db = db;
        _logger = logger;
    }
    public async Task Consume(ConsumeContext<ShipmentCreatedEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation("Processing ShipmentCreated: {TrackingNumber} (ID: {Id})", msg.TrackingNumber, msg.ShipmentId);

        _db.TrackingEvents.Add(new TrackingEvent
        {
            ShipmentId = msg.ShipmentId,
            TrackingNumber = msg.TrackingNumber,
            Status = "Booked",
            Location = msg.SenderCity,
            Description = "Shipment booked successfully",
            EventTime = msg.CreatedAt,
            UpdatedBy = "system"
        });

        await _db.SaveChangesAsync();
        _logger.LogInformation("Created Booked event for {TrackingNumber}", msg.TrackingNumber);
    }
}