namespace SmartShip.AdminService.DTOs;

public record HubDto(int Id, string Name, string City, string State, string Country, string ContactPhone, bool IsActive);
public record CreateHubRequest(string Name, string City, string State, string Country, string ContactPhone);
public record UpdateHubRequest(string Name, string City, string State, string Country, string ContactPhone, bool IsActive);
public class DashboardMetricsDto
{
    public int TotalShipments { get; set; }
    public int ActiveShipments { get; set; }
    public int DeliveredToday { get; set; }
    public int Exceptions { get; set; }
    public int TotalCustomers { get; set; }
    public string? LastUpdatedAt { get; set; }
}
public record ReportRequest(string ReportType, DateTime FromDate, DateTime ToDate);
public record ReportDto(int Id, string Title, string ReportType, DateTime FromDate, DateTime ToDate, DateTime GeneratedAt, object Data);

public class HubPagedRequest : PagedRequest
{
    public bool? IsActive { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
}

public class ReportPagedRequest : PagedRequest
{
    public string? ReportType { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}