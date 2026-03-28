using MassTransit;
using Microsoft.EntityFrameworkCore;
using SmartShip.Shared.Events;
using SmartShip.ShipmentService.Data;
using SmartShip.ShipmentService.Models;

namespace SmartShip.ShipmentService.Messaging.Consumers;

public class PaymentCompletedConsumer : IConsumer<PaymentCompletedEvent>
{
    private readonly ShipmentDbContext _db;
    private readonly ILogger<PaymentCompletedConsumer> _logger;

    public PaymentCompletedConsumer(ShipmentDbContext db, ILogger<PaymentCompletedConsumer> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PaymentCompletedEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation("PaymentCompleted received for {TrackingNumber} | Method: {Method}",
            msg.TrackingNumber, msg.PaymentMethod);

        var shipment = await _db.Shipments
            .FirstOrDefaultAsync(s => s.TrackingNumber == msg.TrackingNumber);

        if (shipment == null)
        {
            _logger.LogWarning("Shipment not found for TrackingNumber: {TrackingNumber}", msg.TrackingNumber);
            return;
        }

        shipment.Status = ShipmentStatus.Booked;  
        await _db.SaveChangesAsync();

        _logger.LogInformation("Shipment {TrackingNumber} -> Status updated to Booked after payment",
            msg.TrackingNumber);
    }
}