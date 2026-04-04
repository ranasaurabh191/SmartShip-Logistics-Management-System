using MassTransit;
using SmartShip.Shared.Events;
using Microsoft.EntityFrameworkCore;
using SmartShip.AdminService.Infrastructure.Data;

namespace SmartShip.AdminService.Infrastructure.Messaging.Consumers;

public class ShipmentDeliveredConsumer : IConsumer<ShipmentDeliveredEvent>
{
    private readonly AdminDbContext _db;
    private readonly ILogger<ShipmentDeliveredConsumer> _logger;

    public ShipmentDeliveredConsumer(AdminDbContext db, ILogger<ShipmentDeliveredConsumer> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ShipmentDeliveredEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation("Admin: ShipmentDelivered received -> {TrackingNumber}", msg.TrackingNumber);

        var metrics = await _db.DashboardMetrics.FirstOrDefaultAsync();
        if (metrics == null)
        {
            _logger.LogWarning("No DashboardMetrics row found");
            return;
        }

        if (metrics.ActiveShipments > 0)
            metrics.ActiveShipments--;

        metrics.DeliveredToday++;
        metrics.LastUpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync();
        _logger.LogInformation("Metrics -> Active:{Active} DeliveredToday:{DeliveredToday}",
            metrics.ActiveShipments, metrics.DeliveredToday);
    }
}