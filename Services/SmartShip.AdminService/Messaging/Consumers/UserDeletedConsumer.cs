using MassTransit;
using SmartShip.AdminService.Data;
using SmartShip.Shared.Events;
using Microsoft.EntityFrameworkCore;
namespace SmartShip.AdminService.Messaging.Consumers
{
    public class UserDeletedConsumer : IConsumer<UserDeletedEvent>
    {
        private readonly AdminDbContext _db;
        private readonly ILogger<UserDeletedConsumer> _logger;

        public UserDeletedConsumer(AdminDbContext db, ILogger<UserDeletedConsumer> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<UserDeletedEvent> context)
        {
            var msg = context.Message;

            if (!msg.Email.Contains("admin"))  
            {
                if (msg.Role == "CUSTOMER")  
                {
                    var metrics = await _db.DashboardMetrics.FirstOrDefaultAsync();
                    if (metrics != null)
                    {
                        metrics.TotalCustomers = Math.Max(0, metrics.TotalCustomers - 1);
                        metrics.LastUpdatedAt = DateTime.Now;
                        await _db.SaveChangesAsync();
                    }
                }
            }
        }
    }
}
