using MassTransit;
using Microsoft.EntityFrameworkCore;
using SmartShip.AdminService.Domain.Entities;
using SmartShip.AdminService.Infrastructure.Data;
using SmartShip.Shared.Events;

namespace SmartShip.AdminService.Infrastructure.Messaging.Consumers;

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

        //var metrics = await _db.DashboardMetrics.FirstOrDefaultAsync();
        //if (metrics == null)
        //{
        //    _logger.LogWarning("No DashboardMetrics row found");
        //    return;
        //}
        var metrics = await _db.DashboardMetrics.FirstOrDefaultAsync();

        if (metrics == null)
        {
            _logger.LogInformation("No DashboardMetrics row found — creating initial row.");
            metrics = new DashboardMetrics
            {
                TotalShipments = 0,
                ActiveShipments = 0,
                DeliveredToday = 0,
                Exceptions = 0,
                TotalCustomers = 0,
                LastUpdatedAt = DateTime.Now
            };
            await _db.DashboardMetrics.AddAsync(metrics);
            await _db.SaveChangesAsync();
        }
        metrics.TotalShipments++;
        metrics.ActiveShipments++;
        metrics.LastUpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync();
        _logger.LogInformation("Metrics -> Total:{Total} Active:{Active}",
            metrics.TotalShipments, metrics.ActiveShipments);
    }
}