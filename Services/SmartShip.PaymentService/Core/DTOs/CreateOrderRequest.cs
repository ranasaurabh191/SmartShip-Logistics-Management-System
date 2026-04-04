using SmartShip.PaymentService.Domain.Entities.Enums;

namespace SmartShip.PaymentService.Core.DTOs;

public record CreateOrderRequest(
    int ShipmentId,
    PaymentMethod PaymentMethod
);