using SmartShip.PaymentService.Models.Enums;

namespace SmartShip.PaymentService.DTOs;

public record CreateOrderRequest(
    int ShipmentId,
    int CustomerId,
    PaymentMethod PaymentMethod
);