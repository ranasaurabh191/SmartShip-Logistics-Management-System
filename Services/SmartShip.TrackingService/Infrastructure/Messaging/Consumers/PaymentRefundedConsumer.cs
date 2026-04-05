using MassTransit;
using SmartShip.Shared.Events;
using SmartShip.TrackingService.Domain.Entities;
using SmartShip.TrackingService.Infrastructure.Data;

namespace SmartShip.TrackingService.Infrastructure.Messaging.Consumers
{
    public class PaymentRefundedTrackingConsumer : IConsumer<PaymentRefundedEvent>
    {
        private readonly TrackingDbContext _db;

        public PaymentRefundedTrackingConsumer(TrackingDbContext db)
        {
            _db = db;
        }

        public async Task Consume(ConsumeContext<PaymentRefundedEvent> context)
        {
            var msg = context.Message;

            _db.TrackingEvents.Add(new TrackingEvent
            {
                ShipmentId = msg.ShipmentId,
                TrackingNumber = msg.TrackingNumber,
                Status = "Refund",
                Location = "Payment Gateway",
                Description = $"Refund processed: ₹{msg.Amount}",
                EventTime = msg.RefundedAt,
                UpdatedBy = "payment-service"
            });

            await _db.SaveChangesAsync();
        }
    }
}
