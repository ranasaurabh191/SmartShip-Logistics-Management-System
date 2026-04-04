namespace SmartShip.ShipmentService.Core.DTOs;

public class UpdateStatusRequest
{
    public string Status { get; set; } = string.Empty;
    public string? Location { get; set; }

    public string? Resolution { get; set; }
}