using SmartShip.NotificationService.Core.DTOs;
using SmartShip.NotificationService.Domain.Entities;

namespace SmartShip.NotificationService.Core.Interfaces.Repositories;

public interface INotificationRepository
{
    Task AddAsync(Notification notification);
    void Update(Notification notification);
    Task<PagedResponse<Notification>> GetPagedAsync(NotificationPagedRequest req);
    Task<PagedResponse<Notification>> GetPagedByUserAsync(int userId, NotificationPagedRequest req);
}