namespace SmartShip.ShipmentService.Models;

public enum ShipmentStatus
{
    Draft, Booked, PickedUp, InTransit, OutForDelivery, Delivered,
    Delayed, Failed, Returned
}

public enum ShipmentType { Domestic, International, Express, Freight }

public class Shipment
{
    public int Id { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public ShipmentType ShipmentType { get; set; }
    public ShipmentStatus Status { get; set; } = ShipmentStatus.Draft;
    public decimal ShippingRate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PickupScheduledAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public string? Notes { get; set; }

    public int SenderAddressId { get; set; }
    public Address SenderAddress { get; set; } = null!;

    public int ReceiverAddressId { get; set; }
    public Address ReceiverAddress { get; set; } = null!;

    public int PackageId { get; set; }
    public Package Package { get; set; } = null!;
}
