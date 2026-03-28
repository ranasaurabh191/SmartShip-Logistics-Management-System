using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartShip.AdminService.Data;
using SmartShip.AdminService.DTOs;
using SmartShip.AdminService.Models;
using System.Text.Json;

namespace SmartShip.AdminService.Services;

public class AdminService : IAdminService
{
    private readonly AdminDbContext _context;
    private readonly ILogger<AdminService> _logger;

    public AdminService(AdminDbContext context, ILogger<AdminService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<DashboardMetricsDto> GetDashboardAsync()
    {
        _logger.LogInformation("Fetching dashboard metrics...");

        var metrics = await _context.DashboardMetrics.FirstOrDefaultAsync();

        if (metrics == null)
        {
            _logger.LogWarning("No metrics row found, creating default...");
            metrics = new DashboardMetrics
            {
                TotalShipments = 0,
                ActiveShipments = 0,
                DeliveredToday = 0,
                Exceptions = 0,
                TotalCustomers = 0,
                LastUpdatedAt = DateTime.Now
            };
            _context.DashboardMetrics.Add(metrics);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Default metrics row created.");
        }

        _logger.LogInformation(
            "Dashboard metrics → Total:{Total} Active:{Active} DeliveredToday:{DeliveredToday} Exceptions:{Exceptions} Customers:{Customers}",
            metrics.TotalShipments, metrics.ActiveShipments,
            metrics.DeliveredToday, metrics.Exceptions, metrics.TotalCustomers);

        return new DashboardMetricsDto
        {
            TotalShipments = metrics.TotalShipments,
            ActiveShipments = metrics.ActiveShipments,
            DeliveredToday = metrics.DeliveredToday,
            Exceptions = metrics.Exceptions,
            TotalCustomers = metrics.TotalCustomers,
            LastUpdatedAt = metrics.LastUpdatedAt
        };
    }

    public async Task<PagedResponse<HubDto>> GetHubsPagedAsync(HubPagedRequest req)
    {
        _logger.LogInformation("Fetching hubs | Page: {Page} | PageSize: {PageSize} | City: {City} | State: {State} | IsActive: {IsActive}",
            req.Page, req.PageSize, req.City ?? "All", req.State ?? "All", req.IsActive?.ToString() ?? "All");

        try
        {
            var query = _context.Hubs.AsQueryable();

            if (req.IsActive.HasValue)
                query = query.Where(h => h.IsActive == req.IsActive.Value);

            if (!string.IsNullOrEmpty(req.City))
                query = query.Where(h => h.City.Contains(req.City));

            if (!string.IsNullOrEmpty(req.State))
                query = query.Where(h => h.State.Contains(req.State));

            if (!string.IsNullOrEmpty(req.Search))
                query = query.Where(h => h.Name.Contains(req.Search)
                                      || h.City.Contains(req.Search)
                                      || h.State.Contains(req.Search));

            query = req.SortBy?.ToLower() switch
            {
                "name" => req.SortOrder == "asc" ? query.OrderBy(h => h.Name) : query.OrderByDescending(h => h.Name),
                "city" => req.SortOrder == "asc" ? query.OrderBy(h => h.City) : query.OrderByDescending(h => h.City),
                _ => req.SortOrder == "asc" ? query.OrderBy(h => h.CreatedAt) : query.OrderByDescending(h => h.CreatedAt)
            };

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((req.Page - 1) * req.PageSize)
                .Take(req.PageSize)
                .Select(h => new HubDto(h.Id, h.Name, h.City, h.State, h.Country, h.ContactPhone, h.IsActive))
                .ToListAsync();

            _logger.LogInformation("Fetched {Count} of {Total} hubs", items.Count, totalCount);

            return new PagedResponse<HubDto>
            {
                Data = items,
                TotalCount = totalCount,
                Page = req.Page,
                PageSize = req.PageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch hubs");
            throw;
        }
    }

    public async Task<HubDto?> GetHubByIdAsync(int id)
    {
        _logger.LogInformation("Fetching hub by ID: {HubId}", id);

        var h = await _context.Hubs.FindAsync(id);

        if (h == null)
        {
            _logger.LogWarning("Hub not found: ID {HubId}", id);
            return null;
        }

        _logger.LogInformation("Hub found: {HubName} | City: {City}", h.Name, h.City);
        return new HubDto(h.Id, h.Name, h.City, h.State, h.Country, h.ContactPhone, h.IsActive);
    }

    public async Task<HubDto> CreateHubAsync(CreateHubRequest req)
    {
        _logger.LogInformation("Creating hub: {HubName} | City: {City} | State: {State}",
            req.Name, req.City, req.State);

        try
        {
            var hub = new Hub
            {
                Name = req.Name,
                City = req.City,
                State = req.State,
                Country = req.Country,
                ContactPhone = req.ContactPhone
            };

            _context.Hubs.Add(hub);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Hub created: ID {HubId} | {HubName} | {City}", hub.Id, hub.Name, hub.City);
            return new HubDto(hub.Id, hub.Name, hub.City, hub.State, hub.Country, hub.ContactPhone, hub.IsActive);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create hub: {HubName}", req.Name);
            throw;
        }
    }

    public async Task<bool> UpdateHubAsync(int id, UpdateHubRequest req)
    {
        _logger.LogInformation("Updating hub ID: {HubId} | Name: {HubName}", id, req.Name);

        try
        {
            var h = await _context.Hubs.FindAsync(id);
            if (h == null)
            {
                _logger.LogWarning("Hub not found for update: ID {HubId}", id);
                return false;
            }

            h.Name = req.Name; h.City = req.City; h.State = req.State;
            h.Country = req.Country; h.ContactPhone = req.ContactPhone; h.IsActive = req.IsActive;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Hub updated: ID {HubId} | {HubName} | IsActive: {IsActive}",
                id, h.Name, h.IsActive);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update hub ID: {HubId}", id);
            throw;
        }
    }

    public async Task<bool> DeleteHubAsync(int id)
    {
        _logger.LogInformation("Deleting hub ID: {HubId}", id);

        try
        {
            var h = await _context.Hubs.FindAsync(id);
            if (h == null)
            {
                _logger.LogWarning("Hub not found for deletion: ID {HubId}", id);
                return false;
            }

            _context.Hubs.Remove(h);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Hub deleted: ID {HubId} | {HubName}", id, h.Name);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete hub ID: {HubId}", id);
            throw;
        }
    }

    public async Task<ReportDto> GenerateReportAsync(ReportRequest req, string generatedBy)
    {
        _logger.LogInformation("Generating {ReportType} report | From: {From} | To: {To} | By: {GeneratedBy}",
            req.ReportType, req.FromDate, req.ToDate, generatedBy);

        try
        {
            Enum.TryParse<ReportType>(req.ReportType, true, out var rt);

            var metrics = await _context.DashboardMetrics.FirstOrDefaultAsync();

            var data = new
            {
                TotalShipments = metrics?.TotalShipments ?? 0,
                Delivered = metrics?.TotalShipments - metrics?.ActiveShipments ?? 0,
                Exceptions = metrics?.Exceptions ?? 0,
                ActiveShipments = metrics?.ActiveShipments ?? 0,
                GeneratedFrom = req.FromDate,
                GeneratedTo = req.ToDate
            };

            var report = new Report
            {
                Title = $"{req.ReportType} Report ({req.FromDate:d} - {req.ToDate:d})",
                ReportType = rt,
                GeneratedBy = generatedBy,
                FromDate = req.FromDate,
                ToDate = req.ToDate,
                DataJson = JsonSerializer.Serialize(data)
            };

            _context.Reports.Add(report);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Report generated: ID {ReportId} | {Title}",
                report.Id, report.Title);

            return new ReportDto(report.Id, report.Title, report.ReportType.ToString(),
                report.FromDate, report.ToDate, report.GeneratedAt, data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate {ReportType} report", req.ReportType);
            throw;
        }
    }


    public async Task<PagedResponse<ReportDto>> GetReportsPagedAsync(ReportPagedRequest req)
    {
        _logger.LogInformation("Fetching reports | Page: {Page} | PageSize: {PageSize} | Type: {ReportType}",
            req.Page, req.PageSize, req.ReportType ?? "All");

        try
        {
            var query = _context.Reports.AsQueryable();

            if (!string.IsNullOrEmpty(req.ReportType) && Enum.TryParse<ReportType>(req.ReportType, true, out var rt))
                query = query.Where(r => r.ReportType == rt);

            if (req.FromDate.HasValue)
                query = query.Where(r => r.GeneratedAt >= req.FromDate.Value);

            if (req.ToDate.HasValue)
                query = query.Where(r => r.GeneratedAt <= req.ToDate.Value);

            if (!string.IsNullOrEmpty(req.Search))
                query = query.Where(r => r.Title.Contains(req.Search));

            query = req.SortOrder == "asc"
                ? query.OrderBy(r => r.GeneratedAt)
                : query.OrderByDescending(r => r.GeneratedAt);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((req.Page - 1) * req.PageSize)
                .Take(req.PageSize)
                .Select(r => new ReportDto(r.Id, r.Title, r.ReportType.ToString(),
                    r.FromDate, r.ToDate, r.GeneratedAt, r.DataJson))
                .ToListAsync();

            _logger.LogInformation("Fetched {Count} of {Total} reports", items.Count, totalCount);

            return new PagedResponse<ReportDto>
            {
                Data = items,
                TotalCount = totalCount,
                Page = req.Page,
                PageSize = req.PageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch reports");
            throw;
        }
    }
}