namespace SmartShip.Shared.Events;

public class ShipmentCreatedEvent
{
    public int ShipmentId { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string SenderCity { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}