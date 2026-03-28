using MassTransit;
using SmartShip.Shared.Events;
using SmartShip.AdminService.Data;
using Microsoft.EntityFrameworkCore;

namespace SmartShip.AdminService.Messaging.Consumers;

public class ShipmentCancelledConsumer : IConsumer<ShipmentCancelledEvent>
{
    private readonly AdminDbContext _db;
    private readonly ILogger<ShipmentCancelledConsumer> _logger;

    public ShipmentCancelledConsumer(AdminDbContext db, ILogger<ShipmentCancelledConsumer> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ShipmentCancelledEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation("Admin: ShipmentCancelled received -> {TrackingNumber}", msg.TrackingNumber);

        var metrics = await _db.DashboardMetrics.FirstOrDefaultAsync();
        if (metrics == null)
        {
            _logger.LogWarning("No DashboardMetrics row found");
            return;
        }

        if (metrics.ActiveShipments > 0)
            metrics.ActiveShipments--;

        metrics.LastUpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync();
        _logger.LogInformation("Metrics -> Active:{Active}", metrics.ActiveShipments);
    }
}