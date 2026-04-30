namespace SmartShip.PaymentService.Core.DTOs;

public class ShipmentDTOs
{
    public int Id { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public decimal ShippingRate { get; set; }
    public string ShipmentType { get; set; } = "Domestic";
    public bool IsFragile { get; set; }
}