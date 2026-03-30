using Microsoft.EntityFrameworkCore;
using SmartShip.NotificationService.Models;

namespace SmartShip.NotificationService.Data;

public class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options) { }

    public DbSet<Notification> Notifications => Set<Notification>();
}