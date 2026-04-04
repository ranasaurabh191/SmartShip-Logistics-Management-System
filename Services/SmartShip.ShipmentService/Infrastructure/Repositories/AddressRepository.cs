using SmartShip.ShipmentService.Core.Interfaces.Repositories;
using SmartShip.ShipmentService.Domain.Entities;
using SmartShip.ShipmentService.Infrastructure.Data;

namespace SmartShip.ShipmentService.Infrastructure.Repositories;

public class AddressRepository : IAddressRepository
{
    private readonly ShipmentDbContext _context;

    public AddressRepository(ShipmentDbContext context)
    {
        _context = context;
    }

    public async Task AddRangeAsync(params Address[] addresses)
    {
        await _context.Addresses.AddRangeAsync(addresses);
    }
}