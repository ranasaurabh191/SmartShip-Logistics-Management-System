// Tests/Mocks/MockRazorpayClient.cs
using SmartShip.PaymentService.Core.Interfaces.Services;

namespace SmartShip.PaymentService.Tests.Mocks;

public class MockRazorpayClient : IRazorpayClient
{
    public string CreateOrder(decimal amount, int shipmentId) => $"order_MOCK_{shipmentId}_{(int)(amount * 100)}";

    public bool VerifySignature(string orderId, string paymentId, string signature) => true;
}