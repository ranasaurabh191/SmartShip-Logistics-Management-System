using SmartShip.ShipmentService.Core.DTOs;
using SmartShip.ShipmentService.Domain.Enums;

namespace SmartShip.ShipmentService.Core.Interfaces.Services
{
    public interface IShipmentService
    {
        Task<ShipmentResponse> CreateAsync(CreateShipmentRequest req, int customerId);
        Task<ShipmentResponse> GetByIdAsync(int id);
        Task UpdateStatusAsync(int id, UpdateStatusRequest request);
        Task SchedulePickupAsync(int id, int customerId, SchedulePickupRequest request);
        Task ResolveExceptionAsync(int id, string resolution);
        Task<decimal> CalculateRateAsync(double weightKg, ShipmentType type);
        Task<PagedResponse<ShipmentResponse>> GetAllPagedAsync(ShipmentPagedRequest request);
        Task<PagedResponse<ShipmentResponse>> GetMyShipmentsPagedAsync(int customerId, PagedRequest request);
        Task CancelByCustomerAsync(int shipmentId, int customerId, string reason);
        Task<IEnumerable<ShipmentSummaryDto>> GetShipmentSummaryByCustomerAsync(int customerId);
        Task<ShipmentResponse?> GetByTrackingNumberAsync(string trackingNumber);
        Task<IEnumerable<RouteStopDto>> GetRouteAsync(int shipmentId);
        Task<RouteStopDto> AdvanceToNextHubAsync(int shipmentId);
    }
}
