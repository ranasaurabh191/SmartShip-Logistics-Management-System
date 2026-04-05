using MassTransit;
using SmartShip.Shared.Events;
using SmartShip.TrackingService.Domain.Entities;
using SmartShip.TrackingService.Infrastructure.Data;

namespace SmartShip.TrackingService.Infrastructure.Messaging.Consumers
{
    public class PaymentCompletedTrackingConsumer : IConsumer<PaymentCompletedEvent>
    {
        private readonly TrackingDbContext _db;

        public PaymentCompletedTrackingConsumer(TrackingDbContext db)
        {
            _db = db;
        }

        public async Task Consume(ConsumeContext<PaymentCompletedEvent> context)
        {
            var msg = context.Message;

            _db.TrackingEvents.Add(new TrackingEvent
            {
                ShipmentId = msg.ShipmentId,
                TrackingNumber = msg.TrackingNumber,
                Status = "PaymentSuccessful",
                Location = "Payment Gateway",
                Description = $"{msg.PaymentMethod} payment successful",
                EventTime = DateTime.UtcNow,
                UpdatedBy = "payment-service"
            });

            await _db.SaveChangesAsync();
        }
    }
}
