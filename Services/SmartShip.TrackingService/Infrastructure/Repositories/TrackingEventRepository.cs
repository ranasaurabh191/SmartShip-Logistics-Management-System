using Microsoft.EntityFrameworkCore;
using SmartShip.TrackingService.Core.DTOs;
using SmartShip.TrackingService.Core.Interfaces.Repositories;
using SmartShip.TrackingService.Domain.Entities;
using SmartShip.TrackingService.Infrastructure.Data;

namespace SmartShip.TrackingService.Infrastructure.Repositories;

public class TrackingEventRepository : ITrackingEventRepository
{
    private readonly TrackingDbContext _context;

    public TrackingEventRepository(TrackingDbContext context)
    {
        _context = context;
    }
    private static readonly string[] ExcludedStatuses = { "Draft", "PaymentCreated", "PaymentSuccessful" };

    public async Task<PagedResponse<TrackingEvent>> GetAllPagedAsync(TrackingEventPagedRequest req)
    {
        var query = _context.TrackingEvents
            .Where(x => !ExcludedStatuses.Contains(x.Status))
            .OrderByDescending(x => x.EventTime)
            .AsQueryable();

        var total = await query.CountAsync();

        var data = await query
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .ToListAsync();

        return new PagedResponse<TrackingEvent>
        {
            Data = data,
            TotalCount = total,
            Page = req.Page,
            PageSize = req.PageSize
        };
    }
    public async Task<TrackingEvent?> GetRecentDuplicateAsync(string trackingNumber, string status, string location, DateTime sinceTime)
    {
        return await _context.TrackingEvents.FirstOrDefaultAsync(t =>
            t.TrackingNumber == trackingNumber &&
            t.Status == status &&
            t.Location == location &&
            t.EventTime >= sinceTime);
    }

    public async Task AddAsync(TrackingEvent trackingEvent)
    {
        await _context.TrackingEvents.AddAsync(trackingEvent);
    }

    public async Task<PagedResponse<TrackingEvent>> GetByTrackingNumberPagedAsync(string trackingNumber, TrackingEventPagedRequest req)
    {
        var query = _context.TrackingEvents
            .Where(t => t.TrackingNumber == trackingNumber && !ExcludedStatuses.Contains(t.Status))
            .AsQueryable();

        if (!string.IsNullOrEmpty(req.Status))
            query = query.Where(t => t.Status.Contains(req.Status));

        if (req.FromDate.HasValue)
            query = query.Where(t => t.EventTime >= req.FromDate.Value);

        if (req.ToDate.HasValue)
            query = query.Where(t => t.EventTime <= req.ToDate.Value);

        if (!string.IsNullOrEmpty(req.Search))
            query = query.Where(t => t.Location.Contains(req.Search) ||
                                     t.Description.Contains(req.Search));

        query = req.SortOrder == "asc"
            ? query.OrderBy(t => t.EventTime)
            : query.OrderByDescending(t => t.EventTime);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .ToListAsync();

        return new PagedResponse<TrackingEvent>
        {
            Data = items,
            TotalCount = totalCount,
            Page = req.Page,
            PageSize = req.PageSize
        };
    }
}