namespace SmartShip.PaymentService.Core.Interfaces.Services;

public interface IRazorpayClient
{
    string CreateOrder(decimal amount, int shipmentId);
    bool VerifySignature(string orderId, string paymentId, string signature);
}