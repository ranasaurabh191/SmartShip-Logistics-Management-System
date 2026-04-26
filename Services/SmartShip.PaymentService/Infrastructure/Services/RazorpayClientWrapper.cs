using Microsoft.Extensions.Options;
using Razorpay.Api;
using SmartShip.PaymentService.Core.Interfaces.Services;
using SmartShip.PaymentService.Domain.Entities;
using System.Security.Cryptography;
using System.Text;

namespace SmartShip.PaymentService.Infrastructure.Services;

public class RazorpayClientWrapper : IRazorpayClient
{
    private readonly RazorpaySettings _settings;

    public RazorpayClientWrapper(IOptions<RazorpaySettings> options)
        => _settings = options.Value;

    public string CreateOrder(decimal amount, int shipmentId)
    {
        var client = new RazorpayClient(_settings.KeyId, _settings.KeySecret);
        var options = new Dictionary<string, object>
        {
            { "amount",          (int)(amount * 100) },
            { "currency",        "INR" },
            { "receipt",         $"receipt_shipment_{shipmentId}" },
            { "payment_capture", 1 }
        };
        var order = client.Order.Create(options);
        return order["id"].ToString();
    }

    public bool VerifySignature(string orderId, string paymentId, string signature)
    {
        var payload = $"{orderId}|{paymentId}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_settings.KeySecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var generated = BitConverter.ToString(hash).Replace("-", "").ToLower();
        return generated == signature;
    }
}