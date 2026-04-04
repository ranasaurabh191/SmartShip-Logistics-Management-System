using Microsoft.EntityFrameworkCore;
using SmartShip.AdminService.Core.Interfaces.Repositories;
using SmartShip.AdminService.Domain.Entities;
using SmartShip.AdminService.Infrastructure.Data;

namespace SmartShip.AdminService.Infrastructure.Repositories;

public class DashboardMetricsRepository : IDashboardMetricsRepository
{
    private readonly AdminDbContext _context;

    public DashboardMetricsRepository(AdminDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardMetrics?> GetFirstAsync()
        => await _context.DashboardMetrics.FirstOrDefaultAsync();

    public async Task AddAsync(DashboardMetrics metrics)
    {
        _context.DashboardMetrics.Add(metrics);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(DashboardMetrics metrics)
    {
        _context.DashboardMetrics.Update(metrics);
        await _context.SaveChangesAsync();
    }
}