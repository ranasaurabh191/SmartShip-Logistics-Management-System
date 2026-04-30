namespace SmartShip.ShipmentService.Core.DTOs
{
    public class AdminSummaryDto
    {
        public int TotalShipments { get; set; }
        public decimal TotalRevenue { get; set; }
        public int InTransitCount { get; set; }
        public int DeliveredCount { get; set; }
        public int CancelledCount { get; set; }
    }
}
