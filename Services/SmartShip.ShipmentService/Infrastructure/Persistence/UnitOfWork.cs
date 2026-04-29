using Microsoft.EntityFrameworkCore;
using SmartShip.ShipmentService.Core.Interfaces.Persistence;
using SmartShip.ShipmentService.Infrastructure.Data;

namespace SmartShip.ShipmentService.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly ShipmentDbContext _context;

    public UnitOfWork(ShipmentDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public T GetDbContext<T>() where T : DbContext
    {
        return (T)(object)_context;
    }
}