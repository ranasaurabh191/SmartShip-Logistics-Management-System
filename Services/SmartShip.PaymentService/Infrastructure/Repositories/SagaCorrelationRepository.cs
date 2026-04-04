using Microsoft.EntityFrameworkCore;
using SmartShip.PaymentService.Core.Interfaces.Repositories;
using SmartShip.PaymentService.Domain.Entities;
using SmartShip.PaymentService.Infrastructure.Data;

namespace SmartShip.PaymentService.Infrastructure.Repositories;

public class SagaCorrelationRepository : ISagaCorrelationRepository
{
    private readonly PaymentDbContext _context;

    public SagaCorrelationRepository(PaymentDbContext context)
    {
        _context = context;
    }

    public async Task<ShipmentSagaCorrelation?> GetByShipmentIdAsync(int shipmentId)
    {
        return await _context.SagaCorrelations.FirstOrDefaultAsync(x => x.ShipmentId == shipmentId);
    }
}