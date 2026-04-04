using Microsoft.EntityFrameworkCore;
using SmartShip.AdminService.Core.DTOs;
using SmartShip.AdminService.Core.Interfaces.Repositories;
using SmartShip.AdminService.Domain.Entities;
using SmartShip.AdminService.Domain.Enums;
using SmartShip.AdminService.Infrastructure.Data;

namespace SmartShip.AdminService.Infrastructure.Repositories;

public class ReportRepository : IReportRepository
{
    private readonly AdminDbContext _context;

    public ReportRepository(AdminDbContext context)
    {
        _context = context;
    }

    public async Task<Report> AddAsync(Report report)
    {
        _context.Reports.Add(report);
        await _context.SaveChangesAsync();
        return report;
    }

    public async Task<PagedResponse<Report>> GetPagedAsync(ReportPagedRequest req)
    {
        var query = _context.Reports.AsQueryable();

        if (!string.IsNullOrWhiteSpace(req.ReportType) &&
            Enum.TryParse<ReportType>(req.ReportType, true, out var rt))
        {
            query = query.Where(r => r.ReportType == rt);
        }

        if (req.FromDate.HasValue)
            query = query.Where(r => r.GeneratedAt >= req.FromDate.Value);

        if (req.ToDate.HasValue)
            query = query.Where(r => r.GeneratedAt <= req.ToDate.Value);

        if (!string.IsNullOrWhiteSpace(req.Search))
            query = query.Where(r => r.Title.Contains(req.Search));

        query = req.SortOrder == "asc"
            ? query.OrderBy(r => r.GeneratedAt)
            : query.OrderByDescending(r => r.GeneratedAt);

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .ToListAsync();

        return new PagedResponse<Report>
        {
            Data = items,
            TotalCount = totalCount,
            Page = req.Page,
            PageSize = req.PageSize
        };
    }
}