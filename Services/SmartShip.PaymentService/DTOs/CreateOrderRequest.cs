using SmartShip.PaymentService.Models.Enums;

namespace SmartShip.PaymentService.DTOs;

public record CreateOrderRequest(
    int ShipmentId,
    string TrackingNumber,
    int CustomerId,
    decimal Amount,
    PaymentMethod PaymentMethod
);