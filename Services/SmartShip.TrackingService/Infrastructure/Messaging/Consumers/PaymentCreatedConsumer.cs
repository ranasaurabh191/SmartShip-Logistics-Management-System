namespace SmartShip.TrackingService.Infrastructure.Messaging.Consumers
{
    using MassTransit;
    using SmartShip.Shared.Events;
    using SmartShip.TrackingService.Domain.Entities;
    using SmartShip.TrackingService.Infrastructure.Data;

    public class PaymentCreatedConsumer : IConsumer<PaymentCreatedEvent>
    {
        private readonly TrackingDbContext _db;

        public PaymentCreatedConsumer(TrackingDbContext db)
        {
            _db = db;
        }

        public async Task Consume(ConsumeContext<PaymentCreatedEvent> context)
        {
            // Payment events are no longer displayed in the public tracking timeline.
            // Logistics events will start from 'Picked Up'.
            await Task.CompletedTask;
        }
    }
}
