using MassTransit;
using SmartShip.Shared.Events;
using SmartShip.TrackingService.Domain.Entities;
using SmartShip.TrackingService.Infrastructure.Data;

public class ShipmentCreatedConsumer : IConsumer<ShipmentCreatedEvent>
{
    private readonly TrackingDbContext _db;
    private readonly ILogger<ShipmentCreatedConsumer> _logger;

    public ShipmentCreatedConsumer(
        TrackingDbContext db,
        ILogger<ShipmentCreatedConsumer> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ShipmentCreatedEvent> context)
    {
        var msg = context.Message;

        _logger.LogInformation(
            "Processing ShipmentCreated: {TrackingNumber} (ID: {Id}). Skipping timeline event as per new requirements.",
            msg.TrackingNumber,
            msg.ShipmentId);

        // Logic removed: We no longer show 'Draft' or creation events in the public timeline.
        // Logistics events will start from 'Picked Up'.
        await Task.CompletedTask;
    }
}