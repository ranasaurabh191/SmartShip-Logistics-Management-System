using MassTransit;
using SmartShip.Shared.Events;
using SmartShip.ShipmentService.Data;
using Microsoft.EntityFrameworkCore; 

namespace SmartShip.ShipmentService.Messaging.Consumers;

public class UserDeletedConsumer : IConsumer<UserDeletedEvent>
{
    private readonly ShipmentDbContext _db; 
    private readonly ILogger<UserDeletedConsumer> _logger;

    public UserDeletedConsumer(ShipmentDbContext db, ILogger<UserDeletedConsumer> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<UserDeletedEvent> context)
    {
        var userId = context.Message.UserId;
        _logger.LogInformation("Processing UserDeleted event for UserId: {UserId}", userId);

        var shipments = await _db.Shipments
            .Where(s => s.CustomerId == userId)
            .ToListAsync(); 

        var count = shipments.Count;
        _logger.LogInformation("Found {Count} shipments for deleted user {UserId}", count, userId);

        if (count > 0)
        {
            _db.Shipments.RemoveRange(shipments);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Cleaned up {Count} shipments for deleted user {UserId}", count, userId);
        }
        else
        {
            _logger.LogInformation("No shipments found for deleted user {UserId}", userId);
        }
    }
}