using Microsoft.EntityFrameworkCore;
using SmartShip.ShipmentService.Core.DTOs;
using SmartShip.ShipmentService.Core.Interfaces.Repositories;
using SmartShip.ShipmentService.Domain.Entities;
using SmartShip.ShipmentService.Domain.Enums;
using SmartShip.ShipmentService.Infrastructure.Data;

namespace SmartShip.ShipmentService.Infrastructure.Repositories;

public class ShipmentRepository : IShipmentRepository
{
    private readonly ShipmentDbContext _context;

    public ShipmentRepository(ShipmentDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResponse<Shipment>> GetAllPagedAsync(ShipmentPagedRequest req)
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
            "status" => req.SortOrder == "asc" ? query.OrderBy(s => s.Status) : query.OrderBy(s => s.Status),
            "rate" => req.SortOrder == "asc" ? query.OrderBy(s => s.ShippingRate) : query.OrderBy(s => s.ShippingRate),
            _ => req.SortOrder == "asc" ? query.OrderBy(s => s.CreatedAt) : query.OrderBy(s => s.CreatedAt)
        };

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .ToListAsync();

        return new PagedResponse<Shipment>
        {
            Data = items,
            TotalCount = totalCount,
            Page = req.Page,
            PageSize = req.PageSize
        };
    }
    public async Task<IEnumerable<Shipment>> GetByCustomerIdAsync(int customerId)
    => await _context.Shipments
        .Where(s => s.CustomerId == customerId)
        .OrderByDescending(s => s.CreatedAt)
        .ToListAsync();

    public async Task<IEnumerable<Shipment>> GetAllAsync()
    => await _context.Shipments
        .OrderByDescending(s => s.CreatedAt)
        .ToListAsync();

    public async Task<PagedResponse<Shipment>> GetByCustomerPagedAsync(int customerId, PagedRequest req)
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

        return new PagedResponse<Shipment>
        {
            Data = items,
            TotalCount = totalCount,
            Page = req.Page,
            PageSize = req.PageSize
        };
    }

    public async Task<Shipment?> GetByIdWithDetailsAsync(int id)
    {
        return await _context.Shipments
            .Include(s => s.SenderAddress)
            .Include(s => s.ReceiverAddress)
            .Include(s => s.Package)
            .FirstOrDefaultAsync(s => s.Id == id);
    }
    public async Task<Shipment?> GetByTrackingNumberAsync(string trackingNumber)
    {
        return await _context.Shipments
            .Include(s => s.SenderAddress)
            .Include(s => s.ReceiverAddress)
            .Include(s => s.Package)
            .FirstOrDefaultAsync(s => s.TrackingNumber == trackingNumber);
    }
    public async Task<Shipment?> GetByIdAsync(int id)
    {
        return await _context.Shipments
            .Include(s => s.SenderAddress)
            .Include(s => s.ReceiverAddress)
            .Include(s => s.Package)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<Shipment?> GetByIdAndCustomerAsync(int shipmentId, int customerId)
    {
        return await _context.Shipments
            .FirstOrDefaultAsync(s => s.Id == shipmentId && s.CustomerId == customerId);
    }

    public async Task AddAsync(Shipment shipment)
    {
        await _context.Shipments.AddAsync(shipment);
    }

    public void Update(Shipment shipment)
    {
        _context.Shipments.Update(shipment);
    }
}