namespace SmartShip.Shared.Events;

public class ShipmentDeliveredEvent
{
    public int ShipmentId { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public DateTime DeliveredAt { get; set; }
}