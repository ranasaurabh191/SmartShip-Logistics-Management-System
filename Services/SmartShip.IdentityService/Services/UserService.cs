using MassTransit;
using Microsoft.EntityFrameworkCore;
using SmartShip.IdentityService.Data;
using SmartShip.IdentityService.DTOs;
using SmartShip.Shared.Events;
namespace SmartShip.IdentityService.Services;

public class UserService : IUserService
{
    private readonly IdentityDbContext _context;
    private readonly ILogger<UserService> _logger;
    private readonly IPublishEndpoint _publisher;

    public UserService(IdentityDbContext context, ILogger<UserService> logger, IPublishEndpoint publisher)
    {
        _context = context;
        _logger = logger;
        _publisher = publisher;
    }
    public async Task<PagedResponse<UserDto>> GetAllUsersPagedAsync(UserPagedRequest req)
    {
        _logger.LogInformation(
            "Fetching paged users | Page: {Page}, PageSize: {PageSize}, Role: {Role}, IsActive: {IsActive}, Search: {Search}",
            req.Page, req.PageSize, req.Role, req.IsActive, req.Search
        );

        try
        {
            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrEmpty(req.Role))
            {
                _logger.LogInformation("Applying Role filter: {Role}", req.Role);
                query = query.Where(u => u.Role == req.Role.ToUpper());
            }

            if (req.IsActive.HasValue)
            {
                _logger.LogInformation("Applying IsActive filter: {IsActive}", req.IsActive.Value);
                query = query.Where(u => u.IsActive == req.IsActive.Value);
            }

            if (!string.IsNullOrEmpty(req.Search))
            {
                _logger.LogInformation("Applying Search filter: {Search}", req.Search);
                query = query.Where(u =>
                    u.Name.Contains(req.Search) ||
                    u.Email.Contains(req.Search));
            }

            var totalCount = await query.CountAsync();

            _logger.LogInformation("Total users after filtering: {TotalCount}", totalCount);

            var items = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((req.Page - 1) * req.PageSize)
                .Take(req.PageSize)
                .Select(u => new UserDto(
                    u.Id,
                    u.Name,
                    u.Email,
                    u.Phone,
                    u.Role,
                    u.IsActive,
                    u.CreatedAt))
                .ToListAsync();

            _logger.LogInformation("Returning {Count} users for Page {Page}", items.Count, req.Page);

            return new PagedResponse<UserDto>
            {
                Data = items,
                TotalCount = totalCount,
                Page = req.Page,
                PageSize = req.PageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while fetching paged users | Page: {Page}, PageSize: {PageSize}", req.Page, req.PageSize);

            throw;
        }
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

    public async Task<bool> DeleteUserAsync(int userId)
    {
        _logger.LogInformation("Deleting user with ID: {UserId}", userId);

        var user = await _context.Users.FindAsync(userId);

        if (user == null)
        {
            _logger.LogWarning("Delete failed - user not found: {UserId}", userId);
            return false;
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User deleted successfully: {UserId}", userId);

        await _publisher.Publish(new UserDeletedEvent
        {
            UserId = userId,
            Email = user.Email,
            Role = user.Role,
            DeletedAt = DateTime.Now
        });

        _logger.LogInformation("Delete Event published successfully: {UserId}", userId);

        return true;
    }
    
}
