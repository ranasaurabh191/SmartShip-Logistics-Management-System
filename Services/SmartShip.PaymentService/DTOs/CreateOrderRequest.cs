using SmartShip.PaymentService.Models.Enums;

namespace SmartShip.PaymentService.DTOs;

public record CreateOrderRequest(
    int ShipmentId,
    PaymentMethod PaymentMethod
);