namespace SmartShip.NotificationService.Core.DTOs;

public record UserEmailDto(int Id, string Email);
public record ShipmentCustomerDto(int Id, int CustomerId);