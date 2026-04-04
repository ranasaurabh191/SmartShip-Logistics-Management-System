namespace SmartShip.AdminService.Domain.Entities;

public class Report
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public ReportType ReportType { get; set; }
    public string GeneratedBy { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.Now;
    public string DataJson { get; set; } = string.Empty; 
}
