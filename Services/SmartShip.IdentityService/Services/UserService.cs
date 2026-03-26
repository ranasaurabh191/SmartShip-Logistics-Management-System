// UserService.cs
using Microsoft.EntityFrameworkCore;
using SmartShip.IdentityService.Data;
using SmartShip.IdentityService.DTOs;

namespace SmartShip.IdentityService.Services;

public class UserService : IUserService
{
    private readonly IdentityDbContext _context;
    public UserService(IdentityDbContext context) => _context = context;

    public async Task<IEnumerable<UserDto>> GetAllUsersAsync() =>
        await _context.Users.Select(u => new UserDto(u.Id, u.Name, u.Email, u.Phone, u.Role, u.IsActive, u.CreatedAt)).ToListAsync();

    public async Task<UserDto?> GetUserByIdAsync(int id)
    {
        var u = await _context.Users.FindAsync(id);
        return u == null ? null : new UserDto(u.Id, u.Name, u.Email, u.Phone, u.Role, u.IsActive, u.CreatedAt);
    }

    public async Task<bool> UpdateUserAsync(int id, UpdateUserRequest request)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return false;
        user.Name = request.Name;
        user.Phone = request.Phone;
        user.IsActive = request.IsActive;
        user.Role = request.Role;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteUserAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return false;
        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return true;
    }
}
