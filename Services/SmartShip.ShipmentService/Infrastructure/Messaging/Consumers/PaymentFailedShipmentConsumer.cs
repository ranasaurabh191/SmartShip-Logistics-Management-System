using MassTransit;
using SmartShip.Shared.Events;
using SmartShip.ShipmentService.Core.Interfaces.Persistence;
using SmartShip.ShipmentService.Core.Interfaces.Repositories;
using SmartShip.ShipmentService.Domain.Enums;

namespace SmartShip.ShipmentService.Infrastructure.Messaging.Consumers;

public class PaymentFailedShipmentConsumer : IConsumer<UpdateShipmentStatusToPaymentFailedCommand>
{
    private readonly ILogger<PaymentFailedShipmentConsumer> _logger;
    private readonly IShipmentRepository _repo;
    private readonly IUnitOfWork _uow;

    public PaymentFailedShipmentConsumer(IShipmentRepository repo, IUnitOfWork uow, ILogger<PaymentFailedShipmentConsumer> logger)
    {
        _repo = repo;
        _uow = uow;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<UpdateShipmentStatusToPaymentFailedCommand> context)
    {
        var msg = context.Message;
        _logger.LogInformation("UpdateShipmentStatusToPaymentFailedCommand received | ShipmentId: {ShipmentId} | Reason: {Reason}",
            msg.ShipmentId, msg.Reason);

        var shipment = await _repo.GetByIdAsync(msg.ShipmentId);

        if (shipment == null)
        {
            _logger.LogWarning("Shipment {ShipmentId} not found for status update.", msg.ShipmentId);
            return;
        }

        // Only update if it's currently Draft or Booked (or already PaymentFailed)
        if (shipment.Status != ShipmentStatus.Draft && shipment.Status != ShipmentStatus.Booked && shipment.Status != ShipmentStatus.PaymentFailed)
        {
            _logger.LogWarning("Shipment {ShipmentId} is in {Status} — skipping status update to PaymentFailed.",
                msg.ShipmentId, shipment.Status);
            return;
        }

        shipment.Status = ShipmentStatus.PaymentFailed;
        shipment.Notes = (shipment.Notes ?? "") + $" | Payment Failed: {msg.Reason} ({DateTime.Now})";
        
        await _uow.SaveChangesAsync();

        _logger.LogInformation("Shipment {TrackingNumber} marked as PaymentFailed.", msg.TrackingNumber);
    }
}
