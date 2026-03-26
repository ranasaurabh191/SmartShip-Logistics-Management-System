// UserService.cs
using Microsoft.EntityFrameworkCore;
using SmartShip.IdentityService.Data;
using SmartShip.IdentityService.DTOs;

namespace SmartShip.IdentityService.Services;

public class UserService : IUserService
{
    private readonly IdentityDbContext _context;
    private readonly ILogger<UserService> _logger;

    public UserService(IdentityDbContext context, ILogger<UserService> logger)
    {
        _context = context;
        _logger = logger;
    }
    public async Task<PagedResponse<UserDto>> GetAllUsersPagedAsync(UserPagedRequest req)
    {
        var query = _context.Users.AsQueryable();

        if (!string.IsNullOrEmpty(req.Role))
            query = query.Where(u => u.Role == req.Role.ToUpper());

        if (req.IsActive.HasValue)
            query = query.Where(u => u.IsActive == req.IsActive.Value);

        if (!string.IsNullOrEmpty(req.Search))
            query = query.Where(u => u.Name.Contains(req.Search) || u.Email.Contains(req.Search));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .Select(u => new UserDto(u.Id, u.Name, u.Email, u.Phone, u.Role, u.IsActive, u.CreatedAt))
            .ToListAsync();

        return new PagedResponse<UserDto>
        {
            Data = items,
            TotalCount = totalCount,
            Page = req.Page,
            PageSize = req.PageSize
        };
    }
    public async Task<UserDto?> GetUserByIdAsync(int id)
    {
        _logger.LogInformation("Fetching user with ID: {UserId}", id);

        var u = await _context.Users.FindAsync(id);

        if (u == null)
        {
            _logger.LogWarning("User not found with ID: {UserId}", id);
            return null;
        }

        _logger.LogInformation("User found: {Email}", u.Email);

        return new UserDto(u.Id, u.Name, u.Email, u.Phone, u.Role, u.IsActive, u.CreatedAt);
    }

    public async Task<bool> UpdateUserAsync(int id, UpdateUserRequest request)
    {
        _logger.LogInformation("Updating user with ID: {UserId}", id);

        var user = await _context.Users.FindAsync(id);

        if (user == null)
        {
            _logger.LogWarning("Update failed - user not found: {UserId}", id);
            return false;
        }

        user.Name = request.Name;
        user.Phone = request.Phone;
        user.IsActive = request.IsActive;
        user.Role = request.Role;

        await _context.SaveChangesAsync();

        _logger.LogInformation("User updated successfully: {UserId}", id);

        return true;
    }

    public async Task<bool> DeleteUserAsync(int id)
    {
        _logger.LogInformation("Deleting user with ID: {UserId}", id);

        var user = await _context.Users.FindAsync(id);

        if (user == null)
        {
            _logger.LogWarning("Delete failed - user not found: {UserId}", id);
            return false;
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User deleted successfully: {UserId}", id);

        return true;
    }
    
}
