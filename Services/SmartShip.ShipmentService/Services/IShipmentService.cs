using SmartShip.ShipmentService.DTOs;
using SmartShip.ShipmentService.Models;

namespace SmartShip.ShipmentService.Services
{
    public interface IShipmentService
    {
        Task<ShipmentResponse> CreateAsync(CreateShipmentRequest req, int customerId);
        Task<ShipmentResponse?> GetByIdAsync(int id);
        Task<(bool Success, string? Error)> UpdateStatusAsync(int id, UpdateStatusRequest request);
        Task<bool> ResolveExceptionAsync(int id, string resolution);
        Task<decimal> CalculateRateAsync(double weightKg, ShipmentType type);
        Task<PagedResponse<ShipmentResponse>> GetAllPagedAsync(ShipmentPagedRequest request);
        Task<PagedResponse<ShipmentResponse>> GetMyShipmentsPagedAsync(int customerId, PagedRequest request);
        Task<(bool Success, string? Error)> SchedulePickupAsync(int id, SchedulePickupRequest request);
    }
}
