using MassTransit;
using SmartShip.Shared.Events;
using Microsoft.EntityFrameworkCore;
using SmartShip.NotificationService.Infrastructure.Data;

namespace SmartShip.NotificationService.Infrastructure.Messaging.Consumers;

public class UserDeletedConsumer : IConsumer<UserDeletedEvent>
{
    private readonly NotificationDbContext _db; 
    private readonly ILogger<UserDeletedConsumer> _logger;

    public UserDeletedConsumer(NotificationDbContext db, ILogger<UserDeletedConsumer> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<UserDeletedEvent> context)
    {
        var userId = context.Message.UserId;
        _logger.LogInformation("Processing UserDeleted event for UserId: {UserId}", userId);

        var notifications = await _db.Notifications
            .Where(n => n.UserId == userId)
            .ToListAsync();

        var count = notifications.Count;
        _logger.LogInformation("Found {Count} notifications for deleted user {UserId}", count, userId);

        if (count > 0)
        {
            _db.Notifications.RemoveRange(notifications);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Cleaned up {Count} notifications for deleted user {UserId}", count, userId);
        }
        else
        {
            _logger.LogInformation("No notifications found for deleted user {UserId}", userId);
        }

    }
}