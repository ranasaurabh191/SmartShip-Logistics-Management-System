using SmartShip.ShipmentService.DTOs;
using SmartShip.ShipmentService.Models;

namespace SmartShip.ShipmentService.Services
{
    public interface IShipmentService
    {
        Task<ShipmentResponse> CreateAsync(CreateShipmentRequest req, int customerId);
        Task<IEnumerable<ShipmentResponse>> GetMyShipmentsAsync(int customerId);
        Task<ShipmentResponse?> GetByIdAsync(int id);
        Task<IEnumerable<ShipmentResponse>> GetAllAsync(string? status, string? type);
        Task<bool> UpdateStatusAsync(int id, string status);
        Task<bool> SchedulePickupAsync(int id, DateTime pickupTime);
        Task<bool> ResolveExceptionAsync(int id, string resolution);
        Task<decimal> CalculateRateAsync(double weightKg, ShipmentType type);
    }
}
