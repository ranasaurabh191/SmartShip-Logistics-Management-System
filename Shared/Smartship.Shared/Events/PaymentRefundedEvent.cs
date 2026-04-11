namespace SmartShip.Shared.Events;

public class PaymentRefundedEvent
{
    public int ShipmentId { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public decimal Amount { get; set; }
    public DateTime RefundedAt { get; set; } = DateTime.Now;
}