using MassTransit;
using SmartShip.Shared.Events;
using SmartShip.TrackingService.Models;
using SmartShip.TrackingService.Data;

public class ShipmentCreatedConsumer : IConsumer<ShipmentCreatedEvent>
{
    private readonly TrackingDbContext _db;
    public ShipmentCreatedConsumer(TrackingDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<ShipmentCreatedEvent> context)
    {
        var msg = context.Message;

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
        Console.WriteLine($"[TrackingService] Auto-created Booked event for {msg.TrackingNumber}");
    }
}