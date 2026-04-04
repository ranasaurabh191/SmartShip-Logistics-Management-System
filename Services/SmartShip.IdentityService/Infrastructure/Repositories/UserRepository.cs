using Microsoft.EntityFrameworkCore;
using SmartShip.IdentityService.Core.DTOs;
using SmartShip.IdentityService.Core.Interfaces.Repositories;
using SmartShip.IdentityService.Domain.Entities;
using SmartShip.IdentityService.Infrastructure.Data;

namespace SmartShip.IdentityService.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IdentityDbContext _context;

    public UserRepository(IdentityDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExistsByEmailAsync(string email)
    {
        return await _context.Users.AnyAsync(u => u.Email == email);
    }

    public async Task<bool> ExistsActiveByIdAsync(int userId)
    {
        return await _context.Users.AnyAsync(u => u.Id == userId && u.IsActive);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<User?> GetByEmailForAdminAsync(string email)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        return await _context.Users.FindAsync(id);
    }

    public async Task AddAsync(User user)
    {
        await _context.Users.AddAsync(user);
    }

    public void Update(User user)
    {
        _context.Users.Update(user);
    }

    public void Delete(User user)
    {
        _context.Users.Remove(user);
    }

    public async Task<PagedResponse<User>> GetPagedAsync(UserPagedRequest req)
    {
        var query = _context.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(req.Role))
            query = query.Where(u => u.Role == req.Role.ToUpper());

        if (req.IsActive.HasValue)
            query = query.Where(u => u.IsActive == req.IsActive.Value);

        if (!string.IsNullOrWhiteSpace(req.Search))
            query = query.Where(u =>
                u.Name.Contains(req.Search) ||
                u.Email.Contains(req.Search));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .ToListAsync();

        return new PagedResponse<User>
        {
            Data = items,
            TotalCount = totalCount,
            Page = req.Page,
            PageSize = req.PageSize
        };
    }
}