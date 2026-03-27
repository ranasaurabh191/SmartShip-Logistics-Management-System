using MassTransit;
using SmartShip.Shared.Events;
using SmartShip.ShipmentService.Data;

namespace SmartShip.ShipmentService.Messaging.Consumers;

public class UserDeletedConsumer : IConsumer<UserDeletedEvent>
{
    private readonly ShipmentDbContext _db;

    public UserDeletedConsumer(ShipmentDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<UserDeletedEvent> context)
    {
        var userId = context.Message.UserId;

        var shipments = _db.Shipments.Where(s => s.CustomerId == userId);
        _db.Shipments.RemoveRange(shipments);
        await _db.SaveChangesAsync();

        Console.WriteLine($"[ShipmentService] Cleaned up shipments for deleted user {userId}");
    }
}