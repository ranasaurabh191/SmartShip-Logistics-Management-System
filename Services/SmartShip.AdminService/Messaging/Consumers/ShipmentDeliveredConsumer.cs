using MassTransit;
using SmartShip.Shared.Events;
using SmartShip.AdminService.Data;
using Microsoft.EntityFrameworkCore;

namespace SmartShip.AdminService.Messaging.Consumers;

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
        _logger.LogInformation("Processing ShipmentDelivered: {TrackingNumber}", msg.TrackingNumber);

        var metrics = await _db.DashboardMetrics.FirstOrDefaultAsync();
        if (metrics == null)
        {
            _logger.LogWarning("No DashboardMetrics row found");
            return;
        }

        metrics.TotalShipments++;
        metrics.ActiveShipments--;
        metrics.DeliveredToday++;
        metrics.LastUpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        _logger.LogInformation("Updated metrics: Total={Total}, Active={Active}, DeliveredToday={DeliveredToday}",
            metrics.TotalShipments, metrics.ActiveShipments, metrics.DeliveredToday);
    }
}