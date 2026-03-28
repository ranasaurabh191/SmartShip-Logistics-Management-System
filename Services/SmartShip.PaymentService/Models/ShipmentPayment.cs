using SmartShip.PaymentService.Models.Enums;

namespace SmartShip.PaymentService.Models;

public class ShipmentPayment
{
    public int Id { get; set; }
    public int ShipmentId { get; set; }
    public string TrackingNumber { get; set; } = "";
    public int CustomerId { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
    public string? RazorpayOrderId { get; set; }
    public string? RazorpayPaymentId { get; set; }
    public string? RazorpaySignature { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? PaidAt { get; set; }
}