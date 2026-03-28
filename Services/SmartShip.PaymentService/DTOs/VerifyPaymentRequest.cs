namespace SmartShip.PaymentService.DTOs;

public class VerifyPaymentRequest
{
    public string RazorpayOrderId { get; set; } = "";
    public string RazorpayPaymentId { get; set; } = "";
    public string Signature { get; set; } = "";
    public int? ShipmentId { get; set; }        
    public string PaymentMethod { get; set; } = "Online";  
}