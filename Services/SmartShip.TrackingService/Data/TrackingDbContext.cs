using Microsoft.EntityFrameworkCore;
using SmartShip.TrackingService.Models;

namespace SmartShip.TrackingService.Data;

public class TrackingDbContext : DbContext
{
    public TrackingDbContext(DbContextOptions<TrackingDbContext> options) : base(options) { }

    public DbSet<TrackingEvent> TrackingEvents => Set<TrackingEvent>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DeliveryProof> DeliveryProofs => Set<DeliveryProof>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TrackingEvent>().HasIndex(t => t.TrackingNumber);
        modelBuilder.Entity<Document>().HasIndex(d => d.ShipmentId);
        modelBuilder.Entity<DeliveryProof>().HasIndex(d => d.TrackingNumber).IsUnique();
        modelBuilder.Entity<Document>().Property(d => d.DocumentType).HasConversion<string>();
    }
}
