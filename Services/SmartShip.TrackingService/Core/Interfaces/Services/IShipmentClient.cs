using SmartShip.TrackingService.Core.DTOs;

namespace SmartShip.TrackingService.Core.Interfaces.Services;

public interface IShipmentClient
{
    Task<List<ShipmentSummary>> GetUserShipmentsAsync(int userId, bool isAdmin);
    Task<ShipmentSummary?> GetShipmentByIdAsync(int shipmentId);
}