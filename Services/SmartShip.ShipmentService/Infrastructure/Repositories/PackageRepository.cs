using SmartShip.ShipmentService.Core.Interfaces.Repositories;
using SmartShip.ShipmentService.Domain.Entities;
using SmartShip.ShipmentService.Infrastructure.Data;

namespace SmartShip.ShipmentService.Infrastructure.Repositories;

public class PackageRepository : IPackageRepository
{
    private readonly ShipmentDbContext _context;

    public PackageRepository(ShipmentDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Package package)
    {
        await _context.Packages.AddAsync(package);
    }
}