using SmartShip.AdminService.Domain.Entities;

namespace SmartShip.AdminService.Core.Interfaces.Repositories;

public interface IDashboardMetricsRepository
{
    Task<DashboardMetrics?> GetFirstAsync();
    Task AddAsync(DashboardMetrics metrics);
    Task UpdateAsync(DashboardMetrics metrics);
}