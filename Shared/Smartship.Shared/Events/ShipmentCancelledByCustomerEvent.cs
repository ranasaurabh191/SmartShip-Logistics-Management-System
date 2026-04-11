namespace SmartShip.Shared.Events;

public class ShipmentCancelledByCustomerEvent
{
    public Guid CorrelationId { get; set; }
    public int ShipmentId { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public decimal Amount { get; set; }
    public bool WasPaid { get; set; }       
    public DateTime CancelledAt { get; set; } = DateTime.Now;
    public string Reason { get; set; } = string.Empty;
}