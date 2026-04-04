using SmartShip.NotificationService.Core.Interfaces.Persistence;
using SmartShip.NotificationService.Infrastructure.Data;

namespace SmartShip.NotificationService.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly NotificationDbContext _context;

    public UnitOfWork(NotificationDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}