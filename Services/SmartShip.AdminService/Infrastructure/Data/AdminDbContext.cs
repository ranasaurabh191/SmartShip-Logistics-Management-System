using Microsoft.EntityFrameworkCore;
using SmartShip.AdminService.Domain.Entities;

namespace SmartShip.AdminService.Infrastructure.Data;

public class AdminDbContext : DbContext
{
    public AdminDbContext(DbContextOptions<AdminDbContext> options) : base(options) { }

    public DbSet<Hub> Hubs => Set<Hub>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<DashboardMetrics> DashboardMetrics => Set<DashboardMetrics>(); 

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var seedDate = new DateTime(2026, 1, 1, 0, 0, 0);

        // Keep original seeds (IDs 1-2) for backward compat, add coordinates
        // New hubs start at ID 101+ to avoid conflicts with manually-added hubs
        modelBuilder.Entity<Hub>().HasData(
            new Hub { Id = 1,   Name = "Delhi Hub",          City = "Delhi",          State = "Delhi",           Country = "India", Latitude = 28.6139,  Longitude = 77.2090,  ContactPhone = "9800000001", IsActive = true, CreatedAt = seedDate },
            new Hub { Id = 2,   Name = "Mumbai Hub",         City = "Mumbai",         State = "Maharashtra",     Country = "India", Latitude = 19.0760,  Longitude = 72.8777,  ContactPhone = "9800000002", IsActive = true, CreatedAt = seedDate },
            new Hub { Id = 101, Name = "Bangalore Hub",      City = "Bengaluru",      State = "Karnataka",       Country = "India", Latitude = 12.9716,  Longitude = 77.5946,  ContactPhone = "9800000003", IsActive = true, CreatedAt = seedDate },
            new Hub { Id = 102, Name = "Hyderabad Hub",      City = "Hyderabad",      State = "Telangana",       Country = "India", Latitude = 17.3850,  Longitude = 78.4867,  ContactPhone = "9800000004", IsActive = true, CreatedAt = seedDate },
            new Hub { Id = 103, Name = "Chennai Hub",        City = "Chennai",        State = "Tamil Nadu",      Country = "India", Latitude = 13.0827,  Longitude = 80.2707,  ContactPhone = "9800000005", IsActive = true, CreatedAt = seedDate },
            new Hub { Id = 104, Name = "Kolkata Hub",        City = "Kolkata",        State = "West Bengal",     Country = "India", Latitude = 22.5726,  Longitude = 88.3639,  ContactPhone = "9800000006", IsActive = true, CreatedAt = seedDate },
            new Hub { Id = 105, Name = "Jalandhar Hub",      City = "Jalandhar",      State = "Punjab",          Country = "India", Latitude = 31.3260,  Longitude = 75.5762,  ContactPhone = "9800000007", IsActive = true, CreatedAt = seedDate },
            new Hub { Id = 106, Name = "Lucknow Hub",        City = "Lucknow",        State = "Uttar Pradesh",   Country = "India", Latitude = 26.8467,  Longitude = 80.9462,  ContactPhone = "9800000008", IsActive = true, CreatedAt = seedDate },
            new Hub { Id = 107, Name = "Pune Hub",           City = "Pune",           State = "Maharashtra",     Country = "India", Latitude = 18.5204,  Longitude = 73.8567,  ContactPhone = "9800000009", IsActive = true, CreatedAt = seedDate },
            new Hub { Id = 108, Name = "Ahmedabad Hub",      City = "Ahmedabad",      State = "Gujarat",         Country = "India", Latitude = 23.0225,  Longitude = 72.5714,  ContactPhone = "9800000010", IsActive = true, CreatedAt = seedDate },
            new Hub { Id = 109, Name = "Jaipur Hub",         City = "Jaipur",         State = "Rajasthan",       Country = "India", Latitude = 26.9124,  Longitude = 75.7873,  ContactPhone = "9800000011", IsActive = true, CreatedAt = seedDate },
            new Hub { Id = 110, Name = "Chandigarh Hub",     City = "Chandigarh",     State = "Chandigarh",      Country = "India", Latitude = 30.7333,  Longitude = 76.7794,  ContactPhone = "9800000012", IsActive = true, CreatedAt = seedDate },
            new Hub { Id = 111, Name = "Indore Hub",         City = "Indore",         State = "Madhya Pradesh",  Country = "India", Latitude = 22.7196,  Longitude = 75.8577,  ContactPhone = "9800000013", IsActive = true, CreatedAt = seedDate },
            new Hub { Id = 112, Name = "Nagpur Hub",         City = "Nagpur",         State = "Maharashtra",     Country = "India", Latitude = 21.1458,  Longitude = 79.0882,  ContactPhone = "9800000014", IsActive = true, CreatedAt = seedDate },
            new Hub { Id = 113, Name = "Patna Hub",          City = "Patna",          State = "Bihar",           Country = "India", Latitude = 25.6093,  Longitude = 85.1376,  ContactPhone = "9800000015", IsActive = true, CreatedAt = seedDate },
            new Hub { Id = 114, Name = "Bhopal Hub",         City = "Bhopal",         State = "Madhya Pradesh",  Country = "India", Latitude = 23.2599,  Longitude = 77.4126,  ContactPhone = "9800000016", IsActive = true, CreatedAt = seedDate },
            new Hub { Id = 115, Name = "Kochi Hub",          City = "Kochi",          State = "Kerala",          Country = "India", Latitude = 9.9312,   Longitude = 76.2673,  ContactPhone = "9800000017", IsActive = true, CreatedAt = seedDate },
            new Hub { Id = 116, Name = "Guwahati Hub",       City = "Guwahati",       State = "Assam",           Country = "India", Latitude = 26.1445,  Longitude = 91.7362,  ContactPhone = "9800000018", IsActive = true, CreatedAt = seedDate },
            new Hub { Id = 117, Name = "Coimbatore Hub",     City = "Coimbatore",     State = "Tamil Nadu",      Country = "India", Latitude = 11.0168,  Longitude = 76.9558,  ContactPhone = "9800000019", IsActive = true, CreatedAt = seedDate },
            new Hub { Id = 118, Name = "Visakhapatnam Hub",  City = "Visakhapatnam",  State = "Andhra Pradesh",  Country = "India", Latitude = 17.6868,  Longitude = 83.2185,  ContactPhone = "9800000020", IsActive = true, CreatedAt = seedDate }
        );

        modelBuilder.Entity<DashboardMetrics>().HasData(
            new DashboardMetrics
            {
                Id = 1,
                TotalShipments = 0,
                ActiveShipments = 0,
                DeliveredToday = 0,
                Exceptions = 0,
                TotalCustomers = 0,
                LastUpdatedAt = seedDate
            }
        );
    }
}