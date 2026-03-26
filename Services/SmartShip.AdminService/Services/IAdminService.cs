using SmartShip.AdminService.DTOs;

public interface IAdminService
{
    Task<DashboardMetrics> GetDashboardAsync();
    Task<PagedResponse<HubDto>> GetHubsPagedAsync(HubPagedRequest request);
    Task<HubDto?> GetHubByIdAsync(int id);
    Task<HubDto> CreateHubAsync(CreateHubRequest req);
    Task<bool> UpdateHubAsync(int id, UpdateHubRequest req);
    Task<bool> DeleteHubAsync(int id);
    Task<PagedResponse<ReportDto>> GetReportsPagedAsync(ReportPagedRequest request);
    Task<ReportDto> GenerateReportAsync(ReportRequest req, string generatedBy);

   
}