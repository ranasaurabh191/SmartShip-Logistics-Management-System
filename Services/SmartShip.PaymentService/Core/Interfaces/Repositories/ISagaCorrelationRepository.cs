using SmartShip.PaymentService.Domain.Entities;

namespace SmartShip.PaymentService.Core.Interfaces.Repositories;

public interface ISagaCorrelationRepository
{
    Task<ShipmentSagaCorrelation?> GetByShipmentIdAsync(int shipmentId);
}