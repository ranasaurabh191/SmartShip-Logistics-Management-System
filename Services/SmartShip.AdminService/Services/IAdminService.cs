using SmartShip.AdminService.DTOs;

public interface IAdminService
{
    Task<DashboardMetrics> GetDashboardAsync();
    Task<IEnumerable<HubDto>> GetHubsAsync();
    Task<HubDto?> GetHubByIdAsync(int id);
    Task<HubDto> CreateHubAsync(CreateHubRequest req);
    Task<bool> UpdateHubAsync(int id, UpdateHubRequest req);
    Task<bool> DeleteHubAsync(int id);
    Task<ReportDto> GenerateReportAsync(ReportRequest req, string generatedBy);
    Task<IEnumerable<ReportDto>> GetReportsAsync();
}