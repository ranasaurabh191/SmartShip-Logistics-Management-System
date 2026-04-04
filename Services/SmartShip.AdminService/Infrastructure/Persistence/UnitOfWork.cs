using SmartShip.AdminService.Core.Interfaces.Persistence;
using SmartShip.AdminService.Infrastructure.Data;

namespace SmartShip.AdminService.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly AdminDbContext _context;

    public UnitOfWork(AdminDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}