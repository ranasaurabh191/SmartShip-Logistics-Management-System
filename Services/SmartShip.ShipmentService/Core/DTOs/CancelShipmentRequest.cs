namespace SmartShip.ShipmentService.Core.DTOs;

public class CancelShipmentRequest
{
    public string Reason { get; set; } = "Cancelled by customer";
}