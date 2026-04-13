namespace SmartShip.IdentityService.Core.DTOs
{
    public class ShipmentSummaryDto
    {
        public int Id { get; set; }
        public string TrackingNumber { get; set; } = string.Empty;
    }
}
