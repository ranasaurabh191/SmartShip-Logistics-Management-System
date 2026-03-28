using SmartShip.PaymentService.DTOs;

namespace SmartShip.PaymentService.Services;

public interface IPaymentService
{
    Task<PaymentResponse?> CreateOrderAsync(CreateOrderRequest request);
    Task<PaymentResponse?> VerifyPaymentAsync(VerifyPaymentRequest request);
    Task<PaymentResponse?> GetByShipmentIdAsync(int shipmentId);
    Task<PaymentResponse?> PaymentStatusAsync(PaymentStatusRequest request);
}