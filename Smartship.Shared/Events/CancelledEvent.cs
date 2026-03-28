namespace SmartShip.Shared.Events;

public class ShipmentCancelledEvent
{
    public int ShipmentId { get; set; }
    public string TrackingNumber { get; set; } = "";
    public DateTime CancelledAt { get; set; } = DateTime.Now;
}