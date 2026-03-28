using MassTransit;
using SmartShip.Shared.Events;
using SmartShip.PaymentService.Data;
using SmartShip.PaymentService.Models;
using SmartShip.PaymentService.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace SmartShip.PaymentService.Messaging.Consumers;

public class ShipmentCreatedConsumer : IConsumer<ShipmentCreatedEvent>
{
    private readonly PaymentDbContext _db;
    private readonly ILogger<ShipmentCreatedConsumer> _logger;

    public ShipmentCreatedConsumer(PaymentDbContext db, ILogger<ShipmentCreatedConsumer> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ShipmentCreatedEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation("PaymentService: ShipmentCreated received → {TrackingNumber}", msg.TrackingNumber);

        var existing = await _db.Payments
            .FirstOrDefaultAsync(p => p.ShipmentId == msg.ShipmentId);

        if (existing != null)
        {
            _logger.LogWarning("Payment record already exists for Shipment {ShipmentId}", msg.ShipmentId);
            return;
        }

        var payment = new ShipmentPayment
        {
            ShipmentId = msg.ShipmentId,
            TrackingNumber = msg.TrackingNumber,
            CustomerId = msg.CustomerId,
            Amount = 0,             
            PaymentMethod = PaymentMethod.COD,      
            PaymentStatus = PaymentStatus.Pending,
            CreatedAt = DateTime.Now
        };

        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Payment record created for {TrackingNumber} | Status: Pending",
            msg.TrackingNumber);
    }
}