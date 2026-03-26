using SmartShip.ShipmentService.DTOs;
using SmartShip.ShipmentService.Models;

namespace SmartShip.ShipmentService.Services
{
    public interface IShipmentService
    {
        Task<ShipmentResponse> CreateAsync(CreateShipmentRequest req, int customerId);
        Task<ShipmentResponse?> GetByIdAsync(int id);
        Task<bool> UpdateStatusAsync(int id, string status);
        Task<bool> SchedulePickupAsync(int id, DateTime pickupTime);
        Task<bool> ResolveExceptionAsync(int id, string resolution);
        Task<decimal> CalculateRateAsync(double weightKg, ShipmentType type);
        Task<PagedResponse<ShipmentResponse>> GetAllPagedAsync(ShipmentPagedRequest request);
        Task<PagedResponse<ShipmentResponse>> GetMyShipmentsPagedAsync(int customerId, PagedRequest request);
    }
}
