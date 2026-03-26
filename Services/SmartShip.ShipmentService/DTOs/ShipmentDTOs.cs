using SmartShip.ShipmentService.Models;

namespace SmartShip.ShipmentService.DTOs;

public record AddressDto(string FullName, string Phone, string Street, string City, string State, string PostalCode, string Country);
public record PackageDto(double WeightKg, double LengthCm, double WidthCm, double HeightCm, string Description, decimal DeclaredValue);

public record CreateShipmentRequest(
    AddressDto SenderAddress,
    AddressDto ReceiverAddress,
    PackageDto Package,
    ShipmentType ShipmentType,
    DateTime? PickupScheduledAt,
    string? Notes
);

public record ShipmentResponse(
    int Id, string TrackingNumber, int CustomerId,
    string ShipmentType, string Status, decimal ShippingRate,
    DateTime CreatedAt, DateTime? PickupScheduledAt, DateTime? DeliveredAt,
    AddressDto SenderAddress, AddressDto ReceiverAddress, PackageDto Package, string? Notes
);

public record UpdateStatusRequest(string Status);
public record SchedulePickupRequest(DateTime PickupScheduledAt);
