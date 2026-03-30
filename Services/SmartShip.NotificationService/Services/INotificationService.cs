using SmartShip.NotificationService.DTOs;

namespace SmartShip.NotificationService.Services;

public interface INotificationService
{
    Task SendAndSaveAsync(int userId, string email, string type, string subject, string body);
    Task<PagedResponse<NotificationDto>> GetAllPagedAsync(NotificationPagedRequest req);
    Task<PagedResponse<NotificationDto>> GetMyNotificationsAsync(int userId, NotificationPagedRequest req);
}