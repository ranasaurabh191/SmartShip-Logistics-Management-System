using SmartShip.ShipmentService.Domain.Entities;

namespace SmartShip.ShipmentService.Core.Interfaces.Repositories;

public interface IAddressRepository
{
    Task AddRangeAsync(params Address[] addresses);
}