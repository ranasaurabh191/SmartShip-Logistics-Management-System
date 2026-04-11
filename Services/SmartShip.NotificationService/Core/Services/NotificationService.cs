using SmartShip.NotificationService.Core.DTOs;
using SmartShip.NotificationService.Core.Interfaces.Persistence;
using SmartShip.NotificationService.Core.Interfaces.Repositories;
using SmartShip.NotificationService.Core.Interfaces.Services;
using SmartShip.NotificationService.Domain.Entities;

namespace SmartShip.NotificationService.Core.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        INotificationRepository notificationRepository,
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        ILogger<NotificationService> logger)
    {
        _notificationRepository = notificationRepository;
        _unitOfWork = unitOfWork;
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
            Body = body,
            CreatedAt = DateTime.Now
        };

        await _notificationRepository.AddAsync(notification);
        await _unitOfWork.SaveChangesAsync();

        try
        {
            await _emailService.SendEmailAsync(email, subject, body);
            notification.IsEmailSent = true;
            notification.SentAt = DateTime.Now;

            _logger.LogInformation("Email sent successfully | Type: {Type} | User: {UserId}",
                type, userId);
        }
        catch (Exception ex)
        {
            notification.ErrorMessage = ex.Message;

            _logger.LogError(ex, "Failed to send email | Type: {Type} | User: {UserId}",
                type, userId);
        }

        _notificationRepository.Update(notification);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<PagedResponse<NotificationDto>> GetAllPagedAsync(NotificationPagedRequest req)
    {
        var paged = await _notificationRepository.GetPagedAsync(req);

        return new PagedResponse<NotificationDto>
        {
            Data = paged.Data.Select(MapToDto),
            TotalCount = paged.TotalCount,
            Page = paged.Page,
            PageSize = paged.PageSize
        };
    }

    public async Task<PagedResponse<NotificationDto>> GetMyNotificationsAsync(int userId, NotificationPagedRequest req)
    {
        var paged = await _notificationRepository.GetPagedByUserAsync(userId, req);

        return new PagedResponse<NotificationDto>
        {
            Data = paged.Data.Select(MapToDto),
            TotalCount = paged.TotalCount,
            Page = paged.Page,
            PageSize = paged.PageSize
        };
    }

    private static NotificationDto MapToDto(Notification n) => new(
        n.Id,
        n.UserId,
        n.Email,
        n.Type,
        n.Subject,
        n.IsEmailSent,
        n.ErrorMessage,
        n.CreatedAt.ToString("dd-MMM-yyyy hh:mm tt"),
        n.SentAt?.ToString("dd-MMM-yyyy hh:mm tt")
    );
}