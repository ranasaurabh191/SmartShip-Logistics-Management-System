using Microsoft.EntityFrameworkCore;
using SmartShip.ShipmentService.Core.Interfaces.Repositories;
using SmartShip.ShipmentService.Domain.Entities;
using SmartShip.ShipmentService.Infrastructure.Data;

namespace SmartShip.ShipmentService.Infrastructure.Repositories;

public class ShipmentOrderSagaRepository : IShipmentOrderSagaRepository
{
    private readonly ShipmentDbContext _context;

    public ShipmentOrderSagaRepository(ShipmentDbContext context)
    {
        _context = context;
    }

    public async Task<ShipmentOrderState?> GetByShipmentIdAsync(int shipmentId)
    {
        return await _context.ShipmentOrderSagas
            .FirstOrDefaultAsync(x => x.ShipmentId == shipmentId);
    }
}