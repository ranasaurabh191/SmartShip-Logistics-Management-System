using Microsoft.EntityFrameworkCore;
using SmartShip.NotificationService.Data;
using SmartShip.NotificationService.DTOs;
using SmartShip.NotificationService.Models;

namespace SmartShip.NotificationService.Services;

public class NotificationService : INotificationService
{
    private readonly NotificationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(NotificationDbContext context,
        IEmailService emailService, ILogger<NotificationService> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task SendAndSaveAsync(int userId, string email, string type, string subject, string body)
    {
        _logger.LogInformation("Sending notification | Type: {Type} | User: {UserId} | Email: {Email}",
            type, userId, email);

        var notification = new Notification
        {
            UserId = userId,
            Email = email,
            Type = type,
            Subject = subject,
            Body = body
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        try
        {
            await _emailService.SendEmailAsync(email, subject, body);
            notification.IsEmailSent = true;
            notification.SentAt = DateTime.UtcNow;
            _logger.LogInformation("Email sent successfully | Type: {Type} | User: {UserId}", type, userId);
        }
        catch (Exception ex)
        {
            notification.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Failed to send email | Type: {Type} | User: {UserId}", type, userId);
        }

        await _context.SaveChangesAsync();
    }

    public async Task<PagedResponse<NotificationDto>> GetAllPagedAsync(NotificationPagedRequest req)
    {
        var query = _context.Notifications.AsQueryable();

        if (!string.IsNullOrEmpty(req.Type))
            query = query.Where(n => n.Type == req.Type);

        if (req.IsEmailSent.HasValue)
            query = query.Where(n => n.IsEmailSent == req.IsEmailSent.Value);

        if (!string.IsNullOrEmpty(req.Search))
            query = query.Where(n => n.Email.Contains(req.Search) || n.Subject.Contains(req.Search));

        query = req.SortOrder == "asc"
            ? query.OrderBy(n => n.CreatedAt)
            : query.OrderByDescending(n => n.CreatedAt);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .Select(n => MapToDto(n))
            .ToListAsync();

        return new PagedResponse<NotificationDto>
        {
            Data = items,
            TotalCount = totalCount,
            Page = req.Page,
            PageSize = req.PageSize
        };
    }

    public async Task<PagedResponse<NotificationDto>> GetMyNotificationsAsync(int userId, NotificationPagedRequest req)
    {
        var query = _context.Notifications
            .Where(n => n.UserId == userId)
            .AsQueryable();

        if (!string.IsNullOrEmpty(req.Type))
            query = query.Where(n => n.Type == req.Type);

        if (!string.IsNullOrEmpty(req.Search))
            query = query.Where(n => n.Subject.Contains(req.Search));

        query = req.SortOrder == "asc"
            ? query.OrderBy(n => n.CreatedAt)
            : query.OrderByDescending(n => n.CreatedAt);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .Select(n => MapToDto(n))
            .ToListAsync();

        return new PagedResponse<NotificationDto>
        {
            Data = items,
            TotalCount = totalCount,
            Page = req.Page,
            PageSize = req.PageSize
        };
    }

    private static NotificationDto MapToDto(Notification n) => new(
        n.Id, n.UserId, n.Email, n.Type, n.Subject, n.IsEmailSent, n.ErrorMessage,
        DateTime.SpecifyKind(n.CreatedAt, DateTimeKind.Utc).ToLocalTime().ToString("dd-MMM-yyyy hh:mm tt"),
        n.SentAt.HasValue
            ? DateTime.SpecifyKind(n.SentAt.Value, DateTimeKind.Utc).ToLocalTime().ToString("dd-MMM-yyyy hh:mm tt")
            : null
    );
}