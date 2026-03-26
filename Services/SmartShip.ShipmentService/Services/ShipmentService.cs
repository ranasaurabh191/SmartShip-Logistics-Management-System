using Microsoft.EntityFrameworkCore;
using SmartShip.ShipmentService.Data;
using SmartShip.ShipmentService.DTOs;
using SmartShip.ShipmentService.Models;

namespace SmartShip.ShipmentService.Services;

public class ShipmentService : IShipmentService
{
    private readonly ShipmentDbContext _context;
    public ShipmentService(ShipmentDbContext context) => _context = context;

    public async Task<ShipmentResponse> CreateAsync(CreateShipmentRequest req, int customerId)
    {
        var rate = await CalculateRateAsync(req.Package.WeightKg, req.ShipmentType);

        var sender = MapAddress(req.SenderAddress);
        var receiver = MapAddress(req.ReceiverAddress);
        var package = MapPackage(req.Package);

        _context.Addresses.AddRange(sender, receiver);
        _context.Packages.Add(package);
        await _context.SaveChangesAsync();

        var shipment = new Shipment
        {
            TrackingNumber = GenerateTrackingNumber(),
            CustomerId = customerId,
            ShipmentType = req.ShipmentType,
            Status = ShipmentStatus.Draft,
            ShippingRate = rate,
            SenderAddressId = sender.Id,
            ReceiverAddressId = receiver.Id,
            PackageId = package.Id,
            PickupScheduledAt = req.PickupScheduledAt,
            Notes = req.Notes
        };

        _context.Shipments.Add(shipment);
        await _context.SaveChangesAsync();
        return MapToResponse(shipment, sender, receiver, package);
    }

    public async Task<IEnumerable<ShipmentResponse>> GetMyShipmentsAsync(int customerId)
    {
        var shipments = await _context.Shipments
            .Include(s => s.SenderAddress).Include(s => s.ReceiverAddress).Include(s => s.Package)
            .Where(s => s.CustomerId == customerId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
        return shipments.Select(s => MapToResponse(s, s.SenderAddress, s.ReceiverAddress, s.Package));
    }

    public async Task<ShipmentResponse?> GetByIdAsync(int id)
    {
        var s = await _context.Shipments.Include(s => s.SenderAddress).Include(s => s.ReceiverAddress).Include(s => s.Package).FirstOrDefaultAsync(s => s.Id == id);
        return s == null ? null : MapToResponse(s, s.SenderAddress, s.ReceiverAddress, s.Package);
    }

    public async Task<IEnumerable<ShipmentResponse>> GetAllAsync(string? status, string? type)
    {
        var query = _context.Shipments.Include(s => s.SenderAddress).Include(s => s.ReceiverAddress).Include(s => s.Package).AsQueryable();
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<ShipmentStatus>(status, true, out var st)) query = query.Where(s => s.Status == st);
        if (!string.IsNullOrEmpty(type) && Enum.TryParse<ShipmentType>(type, true, out var tp)) query = query.Where(s => s.ShipmentType == tp);
        var list = await query.OrderByDescending(s => s.CreatedAt).ToListAsync();
        return list.Select(s => MapToResponse(s, s.SenderAddress, s.ReceiverAddress, s.Package));
    }

    public async Task<bool> UpdateStatusAsync(int id, string status)
    {
        var s = await _context.Shipments.FindAsync(id);
        if (s == null || !Enum.TryParse<ShipmentStatus>(status, true, out var st)) return false;
        s.Status = st;
        if (st == ShipmentStatus.Delivered) s.DeliveredAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SchedulePickupAsync(int id, DateTime pickupTime)
    {
        var s = await _context.Shipments.FindAsync(id);
        if (s == null) return false;
        s.PickupScheduledAt = pickupTime;
        s.Status = ShipmentStatus.Booked;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ResolveExceptionAsync(int id, string resolution)
    {
        var s = await _context.Shipments.FindAsync(id);
        if (s == null) return false;
        s.Notes = resolution;
        s.Status = ShipmentStatus.InTransit;
        await _context.SaveChangesAsync();
        return true;
    }

    public Task<decimal> CalculateRateAsync(double weightKg, ShipmentType type)
    {
        decimal rate = type switch
        {
            ShipmentType.Express => (decimal)(weightKg * 150),
            ShipmentType.International => (decimal)(weightKg * 300),
            ShipmentType.Freight => (decimal)(weightKg * 50),
            _ => (decimal)(weightKg * 80)
        };
        return Task.FromResult(Math.Max(rate, 99));
    }

    private static string GenerateTrackingNumber() =>
        "SS" + DateTime.UtcNow.ToString("yyyyMMdd") + Random.Shared.Next(10000, 99999);

    private static Address MapAddress(AddressDto d) => new() { FullName = d.FullName, Phone = d.Phone, Street = d.Street, City = d.City, State = d.State, PostalCode = d.PostalCode, Country = d.Country };
    private static Package MapPackage(PackageDto d) => new() { WeightKg = d.WeightKg, LengthCm = d.LengthCm, WidthCm = d.WidthCm, HeightCm = d.HeightCm, Description = d.Description, DeclaredValue = d.DeclaredValue };

    private static ShipmentResponse MapToResponse(Shipment s, Address sender, Address receiver, Package pkg) => new(
        s.Id, s.TrackingNumber, s.CustomerId, s.ShipmentType.ToString(), s.Status.ToString(), s.ShippingRate,
        s.CreatedAt, s.PickupScheduledAt, s.DeliveredAt,
        new AddressDto(sender.FullName, sender.Phone, sender.Street, sender.City, sender.State, sender.PostalCode, sender.Country),
        new AddressDto(receiver.FullName, receiver.Phone, receiver.Street, receiver.City, receiver.State, receiver.PostalCode, receiver.Country),
        new PackageDto(pkg.WeightKg, pkg.LengthCm, pkg.WidthCm, pkg.HeightCm, pkg.Description, pkg.DeclaredValue),
        s.Notes
    );
}
