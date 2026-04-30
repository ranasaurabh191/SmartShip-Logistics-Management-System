using MassTransit;
using SmartShip.Shared.Events;
using SmartShip.ShipmentService.Core.Interfaces.Persistence;
using SmartShip.ShipmentService.Core.Interfaces.Repositories;
using SmartShip.ShipmentService.Domain.Enums;

namespace SmartShip.ShipmentService.Infrastructure.Messaging.Consumers;

public class PaymentCreatedConsumer : IConsumer<PaymentCreatedEvent>
{
    private readonly IShipmentRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<PaymentCreatedConsumer> _logger;

    public PaymentCreatedConsumer(IShipmentRepository repo, IUnitOfWork uow, ILogger<PaymentCreatedConsumer> logger)
    {
        _repo = repo;
        _uow = uow;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PaymentCreatedEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation("PaymentCreatedEvent received | ShipmentId: {ShipmentId} | Method: {Method}", 
            msg.ShipmentId, msg.PaymentMethod);

        var shipment = await _repo.GetByIdAsync(msg.ShipmentId);
        if (shipment == null)
        {
            _logger.LogWarning("Shipment {ShipmentId} not found for payment method update.", msg.ShipmentId);
            return;
        }

        if (Enum.TryParse<PaymentMethod>(msg.PaymentMethod, true, out var method))
        {
            shipment.PaymentMethod = method;
            await _uow.SaveChangesAsync();
            _logger.LogInformation("Shipment {ShipmentId} payment method updated to {Method}.", msg.ShipmentId, method);
        }
    }
}
