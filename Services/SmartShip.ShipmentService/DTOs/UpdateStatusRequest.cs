namespace SmartShip.ShipmentService.DTOs;

public class UpdateStatusRequest
{
    public string Status { get; set; } = string.Empty;
    public string? Location { get; set; }
}