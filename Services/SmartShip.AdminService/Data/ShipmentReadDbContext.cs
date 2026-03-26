using Microsoft.EntityFrameworkCore;

namespace SmartShip.AdminService.Data;

public class ShipmentReadDbContext : DbContext
{
    public ShipmentReadDbContext(DbContextOptions<ShipmentReadDbContext> options) : base(options) { }
    public DbSet<ShipmentReadModel> Shipments => Set<ShipmentReadModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ShipmentReadModel>(e =>
        {
            e.ToTable("Shipments");  // ← Map to exact table name
            e.HasKey(s => s.Id);
            e.Property(s => s.Status).HasConversion<int>();     
            e.Property(s => s.ShipmentType).HasConversion<int>(); 
        });
    }
}

public class ShipmentReadModel
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public int Status { get; set; }      
    public int ShipmentType { get; set; } 
    public DateTime CreatedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
}