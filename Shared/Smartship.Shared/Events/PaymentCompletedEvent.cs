namespace SmartShip.Shared.Events;

public class PaymentCompletedEvent
{
    public int ShipmentId { get; set; }
    public string TrackingNumber { get; set; } = "";
    public string PaymentMethod { get; set; } = "";  
    public string PaymentStatus { get; set; } = "";  
}