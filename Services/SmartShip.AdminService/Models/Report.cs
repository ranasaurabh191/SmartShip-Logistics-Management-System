// Models/Report.cs
namespace SmartShip.AdminService.Models;

public enum ReportType { Operational, Performance, SLA, Delivery }

public class Report
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public ReportType ReportType { get; set; }
    public string GeneratedBy { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public string DataJson { get; set; } = string.Empty; // Serialized report data
}
