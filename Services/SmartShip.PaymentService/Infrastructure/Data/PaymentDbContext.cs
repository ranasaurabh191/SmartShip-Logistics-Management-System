using Microsoft.EntityFrameworkCore;
using SmartShip.PaymentService.Domain.Entities;

namespace SmartShip.PaymentService.Infrastructure.Data;

public class PaymentDbContext : DbContext
{
    public DbSet<ShipmentPayment> Payments { get; set; }

    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options) { }

    public DbSet<ShipmentSagaCorrelation> SagaCorrelations => Set<ShipmentSagaCorrelation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ShipmentPayment>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Amount).HasPrecision(18, 2);
            entity.HasIndex(p => p.ShipmentId);
            entity.HasIndex(p => p.TrackingNumber).IsUnique();
            entity.Property(p => p.PaymentMethod).HasConversion<string>();
            entity.Property(p => p.PaymentStatus).HasConversion<string>();
        });
        modelBuilder.Entity<ShipmentSagaCorrelation>(e =>
        {
            e.HasKey(x => x.ShipmentId);
            e.Property(x => x.ShipmentId).ValueGeneratedNever();
        });
    }
}