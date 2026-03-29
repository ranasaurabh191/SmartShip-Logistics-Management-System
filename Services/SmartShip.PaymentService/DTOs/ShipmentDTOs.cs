namespace SmartShip.PaymentService.DTOs;

public class ShipmentDTOs
{
    public int Id { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public decimal ShippingRate { get; set; }
}