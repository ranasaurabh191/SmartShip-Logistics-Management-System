namespace SmartShip.PaymentService.DTOs;

public class PaymentResponse
{
    public int Id { get; set; }
    public int? ShipmentId { get; set; }
    public string TrackingNumber { get; set; } = "";
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "";
    public string PaymentStatus { get; set; } = "";
    public string? RazorpayOrderId { get; set; }
    public string? RazorpayPaymentId { get; set; }
    public string CreatedAt { get; set; } = "";
    public string? PaidAt { get; set; }
    public string? Message { get; set; }
}