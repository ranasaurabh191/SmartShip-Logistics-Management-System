using Microsoft.EntityFrameworkCore;
using SmartShip.NotificationService.Core.DTOs;
using SmartShip.NotificationService.Core.Interfaces.Repositories;
using SmartShip.NotificationService.Domain.Entities;
using SmartShip.NotificationService.Infrastructure.Data;

namespace SmartShip.NotificationService.Infrastructure.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly NotificationDbContext _context;

    public NotificationRepository(NotificationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Notification notification)
    {
        await _context.Notifications.AddAsync(notification);
    }

    public void Update(Notification notification)
    {
        _context.Notifications.Update(notification);
    }

    public async Task<PagedResponse<Notification>> GetPagedAsync(NotificationPagedRequest req)
    {
        var query = _context.Notifications.AsQueryable();

        if (!string.IsNullOrWhiteSpace(req.Type))
            query = query.Where(n => n.Type == req.Type);

        if (req.IsEmailSent.HasValue)
            query = query.Where(n => n.IsEmailSent == req.IsEmailSent.Value);

        if (!string.IsNullOrWhiteSpace(req.Search))
            query = query.Where(n => n.Email.Contains(req.Search) || n.Subject.Contains(req.Search));

        query = req.SortOrder == "asc"
            ? query.OrderBy(n => n.CreatedAt)
            : query.OrderByDescending(n => n.CreatedAt);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .ToListAsync();

        return new PagedResponse<Notification>
        {
            Data = items,
            TotalCount = totalCount,
            Page = req.Page,
            PageSize = req.PageSize
        };
    }

    public async Task<PagedResponse<Notification>> GetPagedByUserAsync(int userId, NotificationPagedRequest req)
    {
        var query = _context.Notifications
            .Where(n => n.UserId == userId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(req.Type))
            query = query.Where(n => n.Type == req.Type);

        if (!string.IsNullOrWhiteSpace(req.Search))
            query = query.Where(n => n.Subject.Contains(req.Search));

        query = req.SortOrder == "asc"
            ? query.OrderBy(n => n.CreatedAt)
            : query.OrderByDescending(n => n.CreatedAt);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .ToListAsync();

        return new PagedResponse<Notification>
        {
            Data = items,
            TotalCount = totalCount,
            Page = req.Page,
            PageSize = req.PageSize
        };
    }
}