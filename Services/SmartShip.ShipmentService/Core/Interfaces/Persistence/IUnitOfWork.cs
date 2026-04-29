using Microsoft.EntityFrameworkCore;

namespace SmartShip.ShipmentService.Core.Interfaces.Persistence;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    T GetDbContext<T>() where T : DbContext;
}