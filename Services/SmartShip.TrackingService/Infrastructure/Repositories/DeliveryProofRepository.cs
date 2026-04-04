using Microsoft.EntityFrameworkCore;
using SmartShip.TrackingService.Core.Interfaces.Repositories;
using SmartShip.TrackingService.Domain.Entities;
using SmartShip.TrackingService.Infrastructure.Data;

namespace SmartShip.TrackingService.Infrastructure.Repositories;

public class DeliveryProofRepository : IDeliveryProofRepository
{
    private readonly TrackingDbContext _context;

    public DeliveryProofRepository(TrackingDbContext context)
    {
        _context = context;
    }

    public async Task<DeliveryProof?> GetByShipmentIdAsync(int shipmentId)
    {
        return await _context.DeliveryProofs.FirstOrDefaultAsync(d => d.ShipmentId == shipmentId);
    }

    public async Task<DeliveryProof?> GetByTrackingNumberAsync(string trackingNumber)
    {
        return await _context.DeliveryProofs.FirstOrDefaultAsync(d => d.TrackingNumber == trackingNumber);
    }

    public async Task AddAsync(DeliveryProof proof)
    {
        await _context.DeliveryProofs.AddAsync(proof);
    }
}