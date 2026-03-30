namespace SmartShip.NotificationService.DTOs;

public record UserEmailDto(int Id, string Email);
public record ShipmentCustomerDto(int Id, int CustomerId);