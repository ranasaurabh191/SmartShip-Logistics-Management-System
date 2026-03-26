namespace SmartShip.AdminService.DTOs;

public record HubDto(int Id, string Name, string City, string State, string Country, string ContactPhone, bool IsActive);
public record CreateHubRequest(string Name, string City, string State, string Country, string ContactPhone);
public record UpdateHubRequest(string Name, string City, string State, string Country, string ContactPhone, bool IsActive);
public record DashboardMetrics(int TotalShipments, int ActiveShipments, int DeliveredToday, int Exceptions, int TotalCustomers);
public record ReportRequest(string ReportType, DateTime FromDate, DateTime ToDate);
public record ReportDto(int Id, string Title, string ReportType, DateTime FromDate, DateTime ToDate, DateTime GeneratedAt, object Data);
