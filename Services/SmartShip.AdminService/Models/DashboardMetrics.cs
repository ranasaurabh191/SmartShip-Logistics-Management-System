namespace SmartShip.AdminService.Models;

public class DashboardMetrics
{
    public int Id { get; set; }
    public int TotalShipments { get; set; }
    public int ActiveShipments { get; set; }
    public int DeliveredToday { get; set; }
    public int Exceptions { get; set; }
    public int TotalCustomers { get; set; }
    public DateTime? LastUpdatedAt { get; set; } = DateTime.UtcNow;
}