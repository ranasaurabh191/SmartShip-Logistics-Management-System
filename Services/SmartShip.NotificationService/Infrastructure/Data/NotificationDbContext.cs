using Microsoft.EntityFrameworkCore;
using SmartShip.NotificationService.Domain.Entities;

namespace SmartShip.NotificationService.Infrastructure.Data;

public class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options) { }

    public DbSet<Notification> Notifications => Set<Notification>();
}