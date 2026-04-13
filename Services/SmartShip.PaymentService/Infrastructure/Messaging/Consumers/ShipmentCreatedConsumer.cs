using MassTransit;
using SmartShip.Shared.Events;
using Microsoft.EntityFrameworkCore;
using SmartShip.PaymentService.Domain.Entities;
using SmartShip.PaymentService.Infrastructure.Data;
namespace SmartShip.PaymentService.Infrastructure.Messaging.Consumers;

public class ShipmentCreatedConsumer : IConsumer<ShipmentCreatedEvent>
{
    private readonly PaymentDbContext _context;
    private readonly ILogger<ShipmentCreatedConsumer> _logger;

    public ShipmentCreatedConsumer(PaymentDbContext context, ILogger<ShipmentCreatedConsumer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ShipmentCreatedEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation("ShipmentCreatedEvent received | ShipmentId: {ShipmentId} | CorrelationId: {CorrelationId}",
            msg.ShipmentId, msg.CorrelationId);

        var existing = await _context.SagaCorrelations
            .FirstOrDefaultAsync(x => x.ShipmentId == msg.ShipmentId);

        if (existing != null)
        {
            _logger.LogWarning("SagaCorrelation already exists for ShipmentId {ShipmentId}, skipping.", msg.ShipmentId);
            return;
        }

        _context.SagaCorrelations.Add(new ShipmentSagaCorrelation
        {
            ShipmentId = msg.ShipmentId,
            CustomerId = msg.CustomerId,
            CorrelationId = msg.CorrelationId  
        });

        await _context.SaveChangesAsync();

        _logger.LogInformation("CorrelationId {CorrelationId} stored for Shipment {ShipmentId}",
            msg.CorrelationId, msg.ShipmentId);
    }
}