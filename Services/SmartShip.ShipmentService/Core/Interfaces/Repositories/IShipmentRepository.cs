using SmartShip.ShipmentService.Core.DTOs;
using SmartShip.ShipmentService.Domain.Entities;

namespace SmartShip.ShipmentService.Core.Interfaces.Repositories;

public interface IShipmentRepository
{
    Task<PagedResponse<Shipment>> GetAllPagedAsync(ShipmentPagedRequest request);
    Task<PagedResponse<Shipment>> GetByCustomerPagedAsync(int customerId, PagedRequest request);
    Task<Shipment?> GetByIdWithDetailsAsync(int id);
    Task<Shipment?> GetByIdAsync(int id);
    Task<Shipment?> GetByIdAndCustomerAsync(int shipmentId, int customerId);
    Task AddAsync(Shipment shipment);
    void Update(Shipment shipment);
    Task<IEnumerable<Shipment>> GetByCustomerIdAsync(int customerId);
    Task<Shipment?> GetByTrackingNumberAsync(string trackingNumber);
}