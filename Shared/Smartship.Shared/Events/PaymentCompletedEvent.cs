namespace SmartShip.Shared.Events;

public class PaymentCompletedEvent
{
    public Guid CorrelationId { get; set; }
    public int ShipmentId { get; set; }
    public string TrackingNumber { get; set; } = "";
    public string PaymentMethod { get; set; } = "";  
    public string PaymentStatus { get; set; } = "";
    public int CustomerId { get; set; }
}