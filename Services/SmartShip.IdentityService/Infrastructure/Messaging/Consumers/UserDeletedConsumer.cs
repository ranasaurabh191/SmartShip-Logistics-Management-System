using MassTransit;
using SmartShip.Shared.Events;
using Microsoft.EntityFrameworkCore;
using SmartShip.IdentityService.Infrastructure.Data;

namespace SmartShip.IdentityService.Infrastructure.Messaging.Consumers;

public class UserDeletedConsumer : IConsumer<UserDeletedEvent>
{
    private readonly IdentityDbContext _db; 
    private readonly ILogger<UserDeletedConsumer> _logger;

    public UserDeletedConsumer(IdentityDbContext db, ILogger<UserDeletedConsumer> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<UserDeletedEvent> context)
    {
        var userId = context.Message.UserId;
        _logger.LogInformation("Processing UserDeleted event for UserId: {UserId}", userId);

        var payments = await _db.OtpVerifications
            .Where(s => s.CustomerId == userId)
            .ToListAsync();

        var count = payments.Count;
        _logger.LogInformation("Found {Count} payments for deleted user {UserId}", count, userId);
        if (count > 0)
        {
            _db.OtpVerifications.RemoveRange(payments);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Cleaned up {Count} OTP verifications for deleted user {UserId}", count, userId);
        }
        else
        {
            _logger.LogInformation("No OTP verifications found for deleted user {UserId}", userId);
        }

       
    }
}