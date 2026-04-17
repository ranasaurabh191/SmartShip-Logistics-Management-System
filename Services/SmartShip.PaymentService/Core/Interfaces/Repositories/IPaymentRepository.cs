using SmartShip.PaymentService.Domain.Entities;

namespace SmartShip.PaymentService.Core.Interfaces.Repositories;

public interface IPaymentRepository
{
    Task<ShipmentPayment?> GetByShipmentIdAsync(int shipmentId);
    Task<ShipmentPayment?> GetByOrderAndShipmentAsync(string? razorpayOrderId, int? shipmentId);
    Task<ShipmentPayment?> GetByOrderIdAsync(string razorpayOrderId);
    Task<ShipmentPayment?> GetByTrackingNumberAsync(string trackingNumber);
    Task AddAsync(ShipmentPayment payment);
    void Update(ShipmentPayment payment);
    Task<List<ShipmentPayment>> GetByCustomerIdAsync(int customerId);
    Task<List<ShipmentPayment>> GetAllAsync();
}