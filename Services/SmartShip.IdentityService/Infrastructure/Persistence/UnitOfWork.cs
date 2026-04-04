using Microsoft.EntityFrameworkCore.Storage;
using SmartShip.IdentityService.Core.Interfaces.Persistence;
using SmartShip.IdentityService.Infrastructure.Data;

namespace SmartShip.IdentityService.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly IdentityDbContext _context;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(IdentityDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _context.SaveChangesAsync(cancellationToken);

}