using Microsoft.EntityFrameworkCore;
using SmartShip.AdminService.Core.DTOs;
using SmartShip.AdminService.Core.Interfaces.Repositories;
using SmartShip.AdminService.Domain.Entities;
using SmartShip.AdminService.Infrastructure.Data;

namespace SmartShip.AdminService.Infrastructure.Repositories;

public class HubRepository : IHubRepository
{
    private readonly AdminDbContext _context;

    public HubRepository(AdminDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResponse<Hub>> GetPagedAsync(HubPagedRequest req)
    {
        var query = _context.Hubs.AsQueryable();

        if (req.IsActive.HasValue)
            query = query.Where(h => h.IsActive == req.IsActive.Value);

        if (!string.IsNullOrWhiteSpace(req.City))
            query = query.Where(h => h.City.Contains(req.City));

        if (!string.IsNullOrWhiteSpace(req.State))
            query = query.Where(h => h.State.Contains(req.State));

        if (!string.IsNullOrWhiteSpace(req.Search))
            query = query.Where(h => h.Name.Contains(req.Search) ||
                                     h.City.Contains(req.Search) ||
                                     h.State.Contains(req.Search));

        query = req.SortBy?.ToLower() switch
        {
            "name" => req.SortOrder == "asc" ? query.OrderBy(h => h.Name) : query.OrderByDescending(h => h.Name),
            "city" => req.SortOrder == "asc" ? query.OrderBy(h => h.City) : query.OrderByDescending(h => h.City),
            _ => req.SortOrder == "asc" ? query.OrderBy(h => h.CreatedAt) : query.OrderByDescending(h => h.CreatedAt)
        };

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .ToListAsync();

        return new PagedResponse<Hub>
        {
            Data = items,
            TotalCount = totalCount,
            Page = req.Page,
            PageSize = req.PageSize
        };
    }

    public async Task<Hub?> GetByIdAsync(int id)
        => await _context.Hubs.FindAsync(id);

    public async Task<Hub> AddAsync(Hub hub)
    {
        _context.Hubs.Add(hub);
        await _context.SaveChangesAsync();
        return hub;
    }

    public async Task UpdateAsync(Hub hub)
    {
        _context.Hubs.Update(hub);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Hub hub)
    {
        _context.Hubs.Remove(hub);
        await _context.SaveChangesAsync();
    }
}