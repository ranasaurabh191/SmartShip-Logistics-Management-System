using MassTransit;
using SmartShip.Shared.Events;
using SmartShip.AdminService.Data;
using Microsoft.EntityFrameworkCore;

namespace SmartShip.AdminService.Messaging.Consumers;

public class ShipmentCreatedMetricsConsumer : IConsumer<ShipmentCreatedEvent>
{
    private readonly AdminDbContext _db;
    private readonly ILogger<ShipmentCreatedMetricsConsumer> _logger;

    public ShipmentCreatedMetricsConsumer(AdminDbContext db, ILogger<ShipmentCreatedMetricsConsumer> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ShipmentCreatedEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation("Admin: ShipmentCreated received -> {TrackingNumber}", msg.TrackingNumber);

        var metrics = await _db.DashboardMetrics.FirstOrDefaultAsync();
        if (metrics == null)
        {
            _logger.LogWarning("No DashboardMetrics row found");
            return;
        }

        metrics.TotalShipments++;
        metrics.ActiveShipments++;
        metrics.LastUpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync();
        _logger.LogInformation("Metrics -> Total:{Total} Active:{Active}",
            metrics.TotalShipments, metrics.ActiveShipments);
    }
}