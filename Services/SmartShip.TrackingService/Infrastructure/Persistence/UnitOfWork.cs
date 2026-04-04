using SmartShip.TrackingService.Core.Interfaces.Persistence;
using SmartShip.TrackingService.Infrastructure.Data;

namespace SmartShip.TrackingService.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly TrackingDbContext _context;

    public UnitOfWork(TrackingDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}