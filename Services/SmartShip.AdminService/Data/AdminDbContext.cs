using Microsoft.EntityFrameworkCore;
using SmartShip.AdminService.Models;

namespace SmartShip.AdminService.Data;

public class AdminDbContext : DbContext
{
    public AdminDbContext(DbContextOptions<AdminDbContext> options) : base(options) { }
    public DbSet<Hub> Hubs => Set<Hub>();
    public DbSet<Report> Reports => Set<Report>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Hub>().HasData(
            new Hub { Id = 1, Name = "Delhi Hub", City = "Delhi", State = "Delhi", Country = "India", ContactPhone = "9800000001", IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Hub { Id = 2, Name = "Mumbai Hub", City = "Mumbai", State = "Maharashtra", Country = "India", ContactPhone = "9800000002", IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );
    }
}
