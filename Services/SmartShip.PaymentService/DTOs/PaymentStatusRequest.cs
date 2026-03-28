namespace SmartShip.PaymentService.DTOs;

public class PaymentStatusRequest
{
    public string? RazorpayOrderId { get; set; }
    public int? ShipmentId { get; set; }
    public string? TrackingNumber { get; set; }
}