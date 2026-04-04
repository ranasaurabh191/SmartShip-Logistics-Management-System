using SmartShip.TrackingService.Domain.Entities;

namespace SmartShip.TrackingService.Core.Interfaces.Repositories;

public interface IDeliveryProofRepository
{
    Task<DeliveryProof?> GetByShipmentIdAsync(int shipmentId);
    Task<DeliveryProof?> GetByTrackingNumberAsync(string trackingNumber);
    Task AddAsync(DeliveryProof proof);
}