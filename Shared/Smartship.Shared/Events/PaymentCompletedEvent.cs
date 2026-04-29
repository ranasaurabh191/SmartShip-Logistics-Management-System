namespace SmartShip.Shared.Events;

public class PaymentCompletedEvent
{
    public Guid CorrelationId { get; set; }
    public int ShipmentId { get; set; }
    public string TrackingNumber { get; set; } = "";
    public string PaymentMethod { get; set; } = "";
    public string PaymentStatus { get; set; } = "";
    public int CustomerId { get; set; }

    // Invoice fields
    public decimal Amount { get; set; }
    public string? PaidAt { get; set; }
    public string? RazorpayPaymentId { get; set; }
    public string? RazorpayOrderId { get; set; }
}