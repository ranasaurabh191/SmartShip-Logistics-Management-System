using MassTransit;
using Microsoft.EntityFrameworkCore;
using SmartShip.PaymentService.Domain.Entities.Enums;
using SmartShip.PaymentService.Infrastructure.Data;
using SmartShip.Shared.Events;

namespace SmartShip.PaymentService.Infrastructure.Messaging.Consumers;

public class ShipmentCancelledConsumer : IConsumer<ShipmentCancelledEvent>
{
    private readonly PaymentDbContext _context;
    private readonly ILogger<ShipmentCancelledConsumer> _logger;

    public ShipmentCancelledConsumer(PaymentDbContext context, ILogger<ShipmentCancelledConsumer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ShipmentCancelledEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation("ShipmentCancelledEvent received (Auto-cancel) | ShipmentId: {ShipmentId}", msg.ShipmentId);

        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.ShipmentId == msg.ShipmentId);

        if (payment == null)
        {
            _logger.LogInformation("No payment found for ShipmentId {ShipmentId}. Nothing to update.", msg.ShipmentId);
            return;
        }

        // If it was already paid, mark as refunded. If it was pending, mark as failed/cancelled.
        if (payment.PaymentStatus == PaymentStatus.Paid)
        {
            payment.PaymentStatus = PaymentStatus.Refunded;
            payment.RefundedAt = DateTime.Now;
            _logger.LogInformation("Payment for ShipmentId {ShipmentId} moved to REFUNDED status.", msg.ShipmentId);
        }
        else if (payment.PaymentStatus == PaymentStatus.Pending)
        {
            payment.PaymentStatus = PaymentStatus.Failed; // Or we could add a Cancelled status to PaymentStatus enum
            _logger.LogInformation("Payment for ShipmentId {ShipmentId} moved to FAILED status (cancelled before payment).", msg.ShipmentId);
        }

        await _context.SaveChangesAsync();
    }
}
