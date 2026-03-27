using MassTransit;
using SmartShip.AdminService.Data;
using SmartShip.Shared.Events;

public class ShipmentDeliveredConsumer : IConsumer<ShipmentDeliveredEvent>
{
    private readonly AdminDbContext _db;
    public ShipmentDeliveredConsumer(AdminDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<ShipmentDeliveredEvent> context)
    {
        Console.WriteLine($"[AdminService] Shipment {context.Message.TrackingNumber} delivered at {context.Message.DeliveredAt}");
    }
}