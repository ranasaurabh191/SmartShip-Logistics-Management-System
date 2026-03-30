using SmartShip.AdminService.DTOs;

public interface IAdminService
{
    Task<DashboardMetricsDto> GetDashboardAsync();
    Task<PagedResponse<HubDto>> GetHubsPagedAsync(HubPagedRequest request);
    Task<HubDto> GetHubByIdAsync(int id);
    Task<HubDto> CreateHubAsync(CreateHubRequest req);
    Task UpdateHubAsync(int id, UpdateHubRequest req);
    Task DeleteHubAsync(int id);
    Task<PagedResponse<ReportDto>> GetReportsPagedAsync(ReportPagedRequest request);
    Task<ReportDto> GenerateReportAsync(ReportRequest req, string generatedBy);

   
}