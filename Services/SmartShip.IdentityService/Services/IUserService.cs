// IUserService.cs
using SmartShip.IdentityService.DTOs;

namespace SmartShip.IdentityService.Services;

public interface IUserService
{
    Task<UserDto> GetUserByIdAsync(int id);
    Task UpdateUserAsync(int id, UpdateUserRequest request);
    Task DeleteUserAsync(int id);
    Task<PagedResponse<UserDto>> GetAllUsersPagedAsync(UserPagedRequest request);
    Task<string> GetUserEmailAsync(int userId);
    Task<bool> UserExistsAsync(int userId);
}
