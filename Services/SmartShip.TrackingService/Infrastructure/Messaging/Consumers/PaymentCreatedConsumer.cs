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
            var msg = context.Message;

            _db.TrackingEvents.Add(new TrackingEvent
            {
                ShipmentId = msg.ShipmentId,
                TrackingNumber = msg.TrackingNumber,
                Status = "PaymentCreated",
                Location = "Payment Gateway",
                Description = $"{msg.PaymentMethod} payment initiated",
                EventTime = msg.CreatedAt,
                UpdatedBy = "payment-service"
            });

            await _db.SaveChangesAsync();
        }
    }
}
