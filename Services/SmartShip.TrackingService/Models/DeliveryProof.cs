namespace SmartShip.TrackingService.Models;

public class DeliveryProof
{
    public int Id { get; set; }
    public int ShipmentId { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;
    public string ReceiverName { get; set; } = string.Empty;
    public string? SignatureImagePath { get; set; }
    public string? PhotoPath { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTime DeliveredAt { get; set; } = DateTime.Now;
    public string DeliveredBy { get; set; } = string.Empty;
}
