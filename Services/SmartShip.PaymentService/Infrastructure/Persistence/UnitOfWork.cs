using SmartShip.PaymentService.Core.Interfaces.Persistence;
using SmartShip.PaymentService.Infrastructure.Data;

namespace SmartShip.PaymentService.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly PaymentDbContext _context;

    public UnitOfWork(PaymentDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}