using SmartShip.ShipmentService.Domain.Entities;

namespace SmartShip.ShipmentService.Core.Interfaces.Repositories;

public interface IPackageRepository
{
    Task AddAsync(Package package);
}