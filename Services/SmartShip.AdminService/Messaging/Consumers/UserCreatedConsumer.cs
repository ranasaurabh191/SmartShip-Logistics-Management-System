using MassTransit;
using SmartShip.Shared.Events;
using SmartShip.AdminService.Data;
using Microsoft.EntityFrameworkCore;

namespace SmartShip.AdminService.Messaging.Consumers;

public class UserCreatedConsumer : IConsumer<UserCreatedEvent>
{
    private readonly AdminDbContext _db;
    private readonly ILogger<UserCreatedConsumer> _logger;

    public UserCreatedConsumer(AdminDbContext db, ILogger<UserCreatedConsumer> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<UserCreatedEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation("Processing UserCreated: {Email} ({Role})", msg.Email, msg.Role);

        if (msg.Role == "CUSTOMER")
        {
            var metrics = await _db.DashboardMetrics.FirstOrDefaultAsync();
            if (metrics != null)
            {
                metrics.TotalCustomers++;
                metrics.LastUpdatedAt = DateTime.Now;
                await _db.SaveChangesAsync();
                _logger.LogInformation("TotalCustomers incremented to {Total}", metrics.TotalCustomers);
            }
        }
        else
        {
            _logger.LogInformation("Skipping non-CUSTOMER: {Role}", msg.Role);
        }
    }
}