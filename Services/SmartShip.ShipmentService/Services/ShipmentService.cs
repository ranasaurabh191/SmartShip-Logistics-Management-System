using Microsoft.EntityFrameworkCore;
using SmartShip.Shared.Events;
using MassTransit;
using SmartShip.ShipmentService.Data;
using SmartShip.ShipmentService.DTOs;
using SmartShip.ShipmentService.Models;

namespace SmartShip.ShipmentService.Services;

public class ShipmentService : IShipmentService
{
    private readonly ShipmentDbContext _context;
    private readonly ILogger<ShipmentService> _logger;
    private readonly IPublishEndpoint _publisher;

    public ShipmentService(ShipmentDbContext context, ILogger<ShipmentService> logger, IPublishEndpoint publisher)
    {
        _context = context;
        _logger = logger;
        _publisher = publisher;
    }

    public async Task<PagedResponse<ShipmentResponse>> GetAllPagedAsync(ShipmentPagedRequest req)
    {
        _logger.LogInformation("Fetching all shipments | Page: {Page} | PageSize: {PageSize} | Status: {Status} | Type: {Type}",
            req.Page, req.PageSize, req.Status ?? "All", req.ShipmentType ?? "All");

        try
        {
            var query = _context.Shipments
                .Include(s => s.SenderAddress)
                .Include(s => s.ReceiverAddress)
                .Include(s => s.Package)
                .AsQueryable();

            if (!string.IsNullOrEmpty(req.Status) && Enum.TryParse<ShipmentStatus>(req.Status, true, out var st))
                query = query.Where(s => s.Status == st);

            if (!string.IsNullOrEmpty(req.ShipmentType) && Enum.TryParse<ShipmentType>(req.ShipmentType, true, out var tp))
                query = query.Where(s => s.ShipmentType == tp);

            if (req.FromDate.HasValue)
                query = query.Where(s => s.CreatedAt >= req.FromDate.Value);

            if (req.ToDate.HasValue)
                query = query.Where(s => s.CreatedAt <= req.ToDate.Value);

            if (!string.IsNullOrEmpty(req.Search))
                query = query.Where(s => s.TrackingNumber.Contains(req.Search));

            query = req.SortBy?.ToLower() switch
            {
                "status" => req.SortOrder == "asc" ? query.OrderBy(s => s.Status) : query.OrderByDescending(s => s.Status),
                "rate" => req.SortOrder == "asc" ? query.OrderBy(s => s.ShippingRate) : query.OrderByDescending(s => s.ShippingRate),
                _ => req.SortOrder == "asc" ? query.OrderBy(s => s.CreatedAt) : query.OrderByDescending(s => s.CreatedAt)
            };

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((req.Page - 1) * req.PageSize)
                .Take(req.PageSize)
                .ToListAsync();

            _logger.LogInformation("Fetched {Count} of {Total} shipments", items.Count, totalCount);

            return new PagedResponse<ShipmentResponse>
            {
                Data = items.Select(s => MapToResponse(s, s.SenderAddress, s.ReceiverAddress, s.Package)),
                TotalCount = totalCount,
                Page = req.Page,
                PageSize = req.PageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch all shipments");
            throw;
        }
    }

    public async Task<PagedResponse<ShipmentResponse>> GetMyShipmentsPagedAsync(int customerId, PagedRequest req)
    {
        _logger.LogInformation("Fetching shipments for Customer {CustomerId} | Page: {Page} | PageSize: {PageSize}",
            customerId, req.Page, req.PageSize);

        try
        {
            var query = _context.Shipments
                .Include(s => s.SenderAddress)
                .Include(s => s.ReceiverAddress)
                .Include(s => s.Package)
                .Where(s => s.CustomerId == customerId)
                .AsQueryable();

            if (!string.IsNullOrEmpty(req.Search))
                query = query.Where(s => s.TrackingNumber.Contains(req.Search));

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(s => s.CreatedAt)
                .Skip((req.Page - 1) * req.PageSize)
                .Take(req.PageSize)
                .ToListAsync();

            _logger.LogInformation("Fetched {Count} of {Total} shipments for Customer {CustomerId}",
                items.Count, totalCount, customerId);

            return new PagedResponse<ShipmentResponse>
            {
                Data = items.Select(s => MapToResponse(s, s.SenderAddress, s.ReceiverAddress, s.Package)),
                TotalCount = totalCount,
                Page = req.Page,
                PageSize = req.PageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch shipments for Customer {CustomerId}", customerId);
            throw;
        }
    }

    public async Task<ShipmentResponse> CreateAsync(CreateShipmentRequest req, int customerId)
    {
        _logger.LogInformation("Creating shipment for Customer {CustomerId} | Type: {Type} | Weight: {Weight}kg",
            customerId, req.ShipmentType, req.Package.WeightKg);

        try
        {
            var rate = await CalculateRateAsync(req.Package.WeightKg, req.ShipmentType);
            _logger.LogInformation("Calculated shipping rate: {Rate} for Type: {Type}", rate, req.ShipmentType);

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

            _logger.LogInformation("Shipment created: {TrackingNumber} | Rate: {Rate} | Customer: {CustomerId}",
                shipment.TrackingNumber, rate, customerId);

            await _publisher.Publish(new ShipmentCreatedEvent
            {
                ShipmentId = shipment.Id,
                TrackingNumber = shipment.TrackingNumber,
                CustomerId = shipment.CustomerId,
                SenderCity = sender.City,
                CreatedAt = shipment.CreatedAt
            });

            return MapToResponse(shipment, sender, receiver, package);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create shipment for Customer {CustomerId}", customerId);
            throw;
        }
    }

    public async Task<ShipmentResponse?> GetByIdAsync(int id)
    {
        _logger.LogInformation("Fetching shipment by ID: {ShipmentId}", id);

        var s = await _context.Shipments
            .Include(s => s.SenderAddress)
            .Include(s => s.ReceiverAddress)
            .Include(s => s.Package)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (s == null)
        {
            _logger.LogWarning("Shipment not found: ID {ShipmentId}", id);
            return null;
        }

        _logger.LogInformation("Shipment found: {TrackingNumber} | Status: {Status}", s.TrackingNumber, s.Status);
        return MapToResponse(s, s.SenderAddress, s.ReceiverAddress, s.Package);
    }

    public async Task<bool> UpdateStatusAsync(int id, UpdateStatusRequest request)  
    {
        _logger.LogInformation("Updating status for Shipment {ShipmentId} → {Status}", id, request.Status);

        try
        {
            var s = await _context.Shipments.FindAsync(id);
            if (s == null) return false;

            if (!Enum.TryParse<ShipmentStatus>(request.Status, true, out var st))
            {
                _logger.LogWarning("Invalid status value: {Status}", request.Status);
                return false;
            }

            var oldStatus = s.Status;
            s.Status = st;
            if (st == ShipmentStatus.Delivered) s.DeliveredAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Shipment {TrackingNumber} status: {OldStatus} → {NewStatus}",
                s.TrackingNumber, oldStatus, st);

            await _publisher.Publish(new ShipmentStatusUpdatedEvent
            {
                ShipmentId = s.Id,
                TrackingNumber = s.TrackingNumber,
                OldStatus = oldStatus.ToString(),
                NewStatus = s.Status.ToString(),
                Location = request.Location ?? "Unknown Hub",     
                UpdatedBy = "Agent-" + DateTime.UtcNow.ToString("hhmm"),
                UpdatedAt = DateTime.UtcNow
            });


            if (s.Status == ShipmentStatus.Delivered)
            {
                await _publisher.Publish(new ShipmentDeliveredEvent
                {
                    ShipmentId = s.Id,
                    TrackingNumber = s.TrackingNumber,
                    CustomerId = s.CustomerId,
                    DeliveredAt = DateTime.UtcNow
                });
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update status for Shipment {ShipmentId}", id);
            throw;
        }
    }

    public async Task<bool> SchedulePickupAsync(int id, DateTime pickupTime)
    {
        _logger.LogInformation("Scheduling pickup for Shipment {ShipmentId} at {PickupTime}", id, pickupTime);

        try
        {
            var s = await _context.Shipments.FindAsync(id);
            if (s == null)
            {
                _logger.LogWarning("Shipment not found for pickup scheduling: ID {ShipmentId}", id);
                return false;
            }

            s.PickupScheduledAt = pickupTime;
            s.Status = ShipmentStatus.Booked;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Pickup scheduled for {TrackingNumber} at {PickupTime}",
                s.TrackingNumber, pickupTime);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to schedule pickup for Shipment {ShipmentId}", id);
            throw;
        }
    }

    public async Task<bool> ResolveExceptionAsync(int id, string resolution)
    {
        _logger.LogInformation("Resolving exception for Shipment {ShipmentId}", id);

        try
        {
            var s = await _context.Shipments.FindAsync(id);
            if (s == null)
            {
                _logger.LogWarning("Shipment not found for exception resolution: ID {ShipmentId}", id);
                return false;
            }

            s.Notes = resolution;
            s.Status = ShipmentStatus.InTransit;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Exception resolved for {TrackingNumber} | Resolution: {Resolution}",
                s.TrackingNumber, resolution);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve exception for Shipment {ShipmentId}", id);
            throw;
        }
    }

    public Task<decimal> CalculateRateAsync(double weightKg, ShipmentType type)
    {
        decimal rate = type switch
        {
            ShipmentType.Express => (decimal)(weightKg * 150),
            ShipmentType.International => (decimal)(weightKg * 300),
            ShipmentType.Freight => (decimal)(weightKg * 50),
            ShipmentType.Domestic => (decimal)(weightKg * 80),
            _ => (decimal)(weightKg * 80)
        };
        var finalRate = Math.Max(rate, 99);
        _logger.LogInformation("Rate calculated: ₹{Rate} | Type: {Type} | Weight: {Weight}kg", finalRate, type, weightKg);
        return Task.FromResult(finalRate);
    }


    private static string GenerateTrackingNumber() => "SS" + DateTime.UtcNow.ToString("yyyyMMdd") + Random.Shared.Next(10000, 99999);

    private static Address MapAddress(AddressDto d) => new()
    {
        FullName = d.FullName,
        Phone = d.Phone,
        Street = d.Street,
        City = d.City,
        State = d.State,
        PostalCode = d.PostalCode,
        Country = d.Country
    };

    private static Package MapPackage(PackageDto d) => new()
    {
        WeightKg = d.WeightKg,
        LengthCm = d.LengthCm,
        WidthCm = d.WidthCm,
        HeightCm = d.HeightCm,
        Description = d.Description,
        DeclaredValue = d.DeclaredValue
    };

    private static ShipmentResponse MapToResponse(Shipment s, Address sender, Address receiver, Package pkg) => new(
        s.Id, s.TrackingNumber, s.CustomerId, s.ShipmentType.ToString(), s.Status.ToString(), s.ShippingRate,
        s.CreatedAt, s.PickupScheduledAt, s.DeliveredAt,
        new AddressDto(sender.FullName, sender.Phone, sender.Street, sender.City, sender.State, sender.PostalCode, sender.Country),
        new AddressDto(receiver.FullName, receiver.Phone, receiver.Street, receiver.City, receiver.State, receiver.PostalCode, receiver.Country),
        new PackageDto(pkg.WeightKg, pkg.LengthCm, pkg.WidthCm, pkg.HeightCm, pkg.Description, pkg.DeclaredValue),
        s.Notes
    );
}