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
            // Payment events are no longer displayed in the public tracking timeline.
            // Logistics events will start from 'Picked Up'.
            await Task.CompletedTask;
        }
    }
}
