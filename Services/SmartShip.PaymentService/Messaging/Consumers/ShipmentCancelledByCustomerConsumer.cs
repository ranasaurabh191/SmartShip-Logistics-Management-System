using MassTransit;
using Microsoft.EntityFrameworkCore;
using SmartShip.PaymentService.Data;
using SmartShip.PaymentService.Models.Enums;
using SmartShip.Shared.Events;

namespace SmartShip.PaymentService.Messaging.Consumers;

public class ShipmentCancelledByCustomerConsumer : IConsumer<ShipmentCancelledByCustomerEvent>
{
    private readonly PaymentDbContext _context;
    private readonly IPublishEndpoint _publisher;
    private readonly ILogger<ShipmentCancelledByCustomerConsumer> _logger;

    public ShipmentCancelledByCustomerConsumer(
        PaymentDbContext context,
        IPublishEndpoint publisher,
        ILogger<ShipmentCancelledByCustomerConsumer> logger)
    {
        _context = context;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ShipmentCancelledByCustomerEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation("ShipmentCancelledByCustomerEvent received | ShipmentId: {ShipmentId} | WasPaid: {WasPaid}",
            msg.ShipmentId, msg.WasPaid);

        if (!msg.WasPaid)
        {
            _logger.LogInformation("Shipment {ShipmentId} was not paid. No refund needed.", msg.ShipmentId);
            return;
        }

        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.ShipmentId == msg.ShipmentId
                && p.PaymentStatus == PaymentStatus.Paid);

        if (payment == null)
        {
            _logger.LogWarning("No paid payment found for ShipmentId {ShipmentId}. Skipping refund.", msg.ShipmentId);
            return;
        }

        payment.PaymentStatus = PaymentStatus.Refunded;
        payment.RefundedAt = DateTime.UtcNow;   
        await _context.SaveChangesAsync();

        _logger.LogInformation("Payment refunded for ShipmentId {ShipmentId} | Amount: {Amount}",
            msg.ShipmentId, msg.Amount);

        await _publisher.Publish(new PaymentRefundedEvent
        {
            ShipmentId = msg.ShipmentId,
            TrackingNumber = msg.TrackingNumber,
            CustomerId = msg.CustomerId,
            Amount = msg.Amount,
            RefundedAt = DateTime.UtcNow
        });

        _logger.LogInformation("PaymentRefundedEvent published for ShipmentId {ShipmentId}", msg.ShipmentId);
    }
}