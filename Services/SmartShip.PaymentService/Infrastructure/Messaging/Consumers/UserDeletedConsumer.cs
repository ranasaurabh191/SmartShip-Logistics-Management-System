using MassTransit;
using SmartShip.Shared.Events;
using Microsoft.EntityFrameworkCore;
using SmartShip.PaymentService.Infrastructure.Data;

namespace SmartShip.PaymentService.Infrastructure.Messaging.Consumers;

public class UserDeletedConsumer : IConsumer<UserDeletedEvent>
{
    private readonly PaymentDbContext _db; 
    private readonly ILogger<UserDeletedConsumer> _logger;

    public UserDeletedConsumer(PaymentDbContext db, ILogger<UserDeletedConsumer> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<UserDeletedEvent> context)
    {
        var userId = context.Message.UserId;
        _logger.LogInformation("Processing UserDeleted event for UserId: {UserId}", userId);

        var payments = await _db.Payments
            .Where(s => s.CustomerId == userId)
            .ToListAsync();

        var saga = await _db.SagaCorrelations
            .Where(s => s.CustomerId == userId)
            .ToListAsync();

        var count = payments.Count;
        _logger.LogInformation("Found {Count} payments for deleted user {UserId}", count, userId);
        if (count > 0)
        {
            _db.Payments.RemoveRange(payments);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Cleaned up {Count} payments for deleted user {UserId}", count, userId);
        }
        else
        {
            _logger.LogInformation("No payments found for deleted user {UserId}", userId);
        }

        var sagaCount = saga.Count;
        _logger.LogInformation("Found {Count} saga correlations for deleted user {UserId}", sagaCount, userId);
        if (sagaCount > 0)
        {
            _db.SagaCorrelations.RemoveRange(saga);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Cleaned up {Count} saga correlations for deleted user {UserId}", sagaCount, userId);
        }
        else
        {
            _logger.LogInformation("No saga correlations found for deleted user {UserId}", userId);
        }
    }
}