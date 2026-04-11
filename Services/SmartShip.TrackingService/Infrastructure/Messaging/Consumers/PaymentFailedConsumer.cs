using MassTransit;
using SmartShip.Shared.Events;
using SmartShip.TrackingService.Domain.Entities;
using SmartShip.TrackingService.Infrastructure.Data;

namespace SmartShip.TrackingService.Infrastructure.Messaging.Consumers
{
    public class PaymentFailedTrackingConsumer : IConsumer<PaymentFailedEvent>
    {
        private readonly TrackingDbContext _db;

        public PaymentFailedTrackingConsumer(TrackingDbContext db)
        {
            _db = db;
        }

        public async Task Consume(ConsumeContext<PaymentFailedEvent> context)
        {
            var msg = context.Message;

            _db.TrackingEvents.Add(new TrackingEvent
            {
                ShipmentId = msg.ShipmentId,
                TrackingNumber = msg.TrackingNumber,
                Status = "PaymentFailed",
                Location = "Payment Gateway",
                Description = msg.Reason,
                EventTime = DateTime.Now,
                UpdatedBy = "payment-service"
            });

            await _db.SaveChangesAsync();
        }
    }
}
