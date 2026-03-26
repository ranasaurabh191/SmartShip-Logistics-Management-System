using Microsoft.EntityFrameworkCore;
using SmartShip.AdminService.Data;
using SmartShip.AdminService.DTOs;
using SmartShip.AdminService.Models;
using System.Text.Json;

namespace SmartShip.AdminService.Services;


public class AdminService : IAdminService
{
    private readonly AdminDbContext _context;
    private readonly ShipmentReadDbContext _shipmentContext;

    public AdminService(AdminDbContext context, ShipmentReadDbContext shipmentContext)
    { _context = context; _shipmentContext = shipmentContext; }

    public async Task<DashboardMetrics> GetDashboardAsync()
    {
        var today = DateTime.UtcNow.Date;

        // Status enum values as int:
        // Draft=0, Booked=1, PickedUp=2, InTransit=3, OutForDelivery=4, Delivered=5
        // Delayed=6, Failed=7, Returned=8

        var total = await _shipmentContext.Shipments.CountAsync();

        var active = await _shipmentContext.Shipments
            .CountAsync(s => s.Status != 5 && s.Status != 7 && s.Status != 8);

        var deliveredToday = await _shipmentContext.Shipments
            .CountAsync(s => s.Status == 5 && s.DeliveredAt.HasValue
                          && s.DeliveredAt.Value.Date == today);

        var exceptions = await _shipmentContext.Shipments
            .CountAsync(s => s.Status == 6 || s.Status == 7 || s.Status == 8);

        var customers = await _shipmentContext.Shipments
            .Select(s => s.CustomerId).Distinct().CountAsync();

        return new DashboardMetrics(total, active, deliveredToday, exceptions, customers);
    }


    public async Task<HubDto?> GetHubByIdAsync(int id)
    {
        var h = await _context.Hubs.FindAsync(id);
        return h == null ? null : new HubDto(h.Id, h.Name, h.City, h.State, h.Country, h.ContactPhone, h.IsActive);
    }

    public async Task<HubDto> CreateHubAsync(CreateHubRequest req)
    {
        var hub = new Hub { Name = req.Name, City = req.City, State = req.State, Country = req.Country, ContactPhone = req.ContactPhone };
        _context.Hubs.Add(hub);
        await _context.SaveChangesAsync();
        return new HubDto(hub.Id, hub.Name, hub.City, hub.State, hub.Country, hub.ContactPhone, hub.IsActive);
    }

    public async Task<bool> UpdateHubAsync(int id, UpdateHubRequest req)
    {
        var h = await _context.Hubs.FindAsync(id);
        if (h == null) return false;
        h.Name = req.Name; h.City = req.City; h.State = req.State;
        h.Country = req.Country; h.ContactPhone = req.ContactPhone; h.IsActive = req.IsActive;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteHubAsync(int id)
    {
        var h = await _context.Hubs.FindAsync(id);
        if (h == null) return false;
        _context.Hubs.Remove(h);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<ReportDto> GenerateReportAsync(ReportRequest req, string generatedBy)
    {
        Enum.TryParse<ReportType>(req.ReportType, true, out var rt);

        var shipments = await _shipmentContext.Shipments
            .Where(s => s.CreatedAt >= req.FromDate && s.CreatedAt <= req.ToDate)
            .ToListAsync();

        var data = new
        {
            TotalShipments = shipments.Count,
            Delivered = shipments.Count(s => s.Status == 5),
            Exceptions = shipments.Count(s => s.Status == 6 || s.Status == 7),
            ByStatus = shipments.GroupBy(s => s.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
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

        return new ReportDto(report.Id, report.Title, report.ReportType.ToString(),
            report.FromDate, report.ToDate, report.GeneratedAt, data);
    }
    public async Task<PagedResponse<HubDto>> GetHubsPagedAsync(HubPagedRequest req)
    {
        var query = _context.Hubs.AsQueryable();

        // Filters
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

        // Sorting
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

        return new PagedResponse<HubDto>
        {
            Data = items,
            TotalCount = totalCount,
            Page = req.Page,
            PageSize = req.PageSize
        };
    }

    public async Task<PagedResponse<ReportDto>> GetReportsPagedAsync(ReportPagedRequest req)
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
            .Select(r => new ReportDto(r.Id, r.Title, r.ReportType.ToString(), r.FromDate, r.ToDate, r.GeneratedAt, r.DataJson))
            .ToListAsync();

        return new PagedResponse<ReportDto>
        {
            Data = items,
            TotalCount = totalCount,
            Page = req.Page,
            PageSize = req.PageSize
        };
    }
}
