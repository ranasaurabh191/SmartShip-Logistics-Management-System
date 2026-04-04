using SmartShip.ShipmentService.Domain.Entities;

namespace SmartShip.ShipmentService.Core.Interfaces.Repositories;

public interface IShipmentOrderSagaRepository
{
    Task<ShipmentOrderState?> GetByShipmentIdAsync(int shipmentId);
}