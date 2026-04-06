using Microsoft.EntityFrameworkCore;
using SmartShip.ShipmentService.Domain.Entities;

namespace SmartShip.ShipmentService.Infrastructure.Data;

public class ShipmentDbContext : DbContext
{
    public ShipmentDbContext(DbContextOptions<ShipmentDbContext> options) : base(options) { }

    public DbSet<Shipment> Shipments => Set<Shipment>();
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<Package> Packages => Set<Package>();
    public DbSet<ShipmentOrderState> ShipmentOrderSagas => Set<ShipmentOrderState>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Shipment>(e =>
        {

            e.HasKey(s => s.Id);
            e.HasIndex(s => s.TrackingNumber).IsUnique();
            e.Property(s => s.ShippingRate).HasPrecision(18, 2);
            e.Property(s => s.ShipmentType).HasConversion<string>().HasMaxLength(20);
            e.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);
            e.HasOne(s => s.SenderAddress).WithMany().HasForeignKey(s => s.SenderAddressId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(s => s.ReceiverAddress).WithMany().HasForeignKey(s => s.ReceiverAddressId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(s => s.Package).WithMany().HasForeignKey(s => s.PackageId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<Package>(e =>
        {
            e.Property(p => p.DeclaredValue)
             .HasColumnType("decimal(18,2)");
        });
        modelBuilder.Entity<ShipmentOrderState>(e =>
        {
            e.HasKey(s => s.CorrelationId);
            e.Property(s => s.CurrentState).HasMaxLength(64);
            e.Property(s => s.TrackingNumber).HasMaxLength(50);
            e.Property(s => s.ShipmentIdKey).HasMaxLength(20);
            e.HasIndex(s => s.ShipmentIdKey).IsUnique();
            e.Property(s => s.Amount).HasPrecision(18, 2);
            e.Property(s => s.RowVersion).IsRowVersion();
        });

    }
}
