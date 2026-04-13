using MassTransit;
using SmartShip.Shared.Events;
using Microsoft.EntityFrameworkCore;
using SmartShip.TrackingService.Infrastructure.Data;

namespace SmartShip.TrackingService.Infrastructure.Messaging.Consumers;

public class UserDeletedConsumer : IConsumer<UserDeletedEvent>
{
    private readonly TrackingDbContext _db; 
    private readonly ILogger<UserDeletedConsumer> _logger;

    public UserDeletedConsumer(TrackingDbContext db, ILogger<UserDeletedConsumer> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<UserDeletedEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation("UserDeleted received | UserId: {UserId} | Shipments: {Count}",
            msg.UserId, msg.ShipmentIds.Count);

        if (!msg.ShipmentIds.Any() && !msg.TrackingNumbers.Any())
        {
            _logger.LogInformation("No shipments found for UserId {UserId} — nothing to delete.", msg.UserId);
            return;
        }

        var deliveryProofs = await _db.DeliveryProofs
            .Where(d => msg.ShipmentIds.Contains(d.ShipmentId)
                     || msg.TrackingNumbers.Contains(d.TrackingNumber))
            .ToListAsync();

        var documents = await _db.Documents
            .Where(d => msg.ShipmentIds.Contains(d.ShipmentId)
                     || d.UploadedByUserId == msg.UserId)
            .ToListAsync();

        var trackingEvents = await _db.TrackingEvents
            .Where(t => msg.ShipmentIds.Contains(t.ShipmentId)
                     || msg.TrackingNumbers.Contains(t.TrackingNumber))
            .ToListAsync();

        if (deliveryProofs.Any())
        {
            _db.DeliveryProofs.RemoveRange(deliveryProofs);
            _logger.LogInformation("Removing {Count} DeliveryProofs", deliveryProofs.Count);
        }

        if (documents.Any())
        {
            _db.Documents.RemoveRange(documents);
            _logger.LogInformation("Removing {Count} Documents", documents.Count);
        }

        if (trackingEvents.Any())
        {
            _db.TrackingEvents.RemoveRange(trackingEvents);
            _logger.LogInformation("Removing {Count} TrackingEvents", trackingEvents.Count);
        }

        if (deliveryProofs.Any() || documents.Any() || trackingEvents.Any())
        {
            await _db.SaveChangesAsync();
            _logger.LogInformation("TrackingDB cleanup complete for UserId {UserId}", msg.UserId);
        }
        else
        {
            _logger.LogInformation("No tracking records found for UserId {UserId}", msg.UserId);
        }
    }
}