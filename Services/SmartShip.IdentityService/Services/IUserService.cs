// IUserService.cs
using SmartShip.IdentityService.DTOs;

namespace SmartShip.IdentityService.Services;

public interface IUserService
{
    Task<UserDto?> GetUserByIdAsync(int id);
    Task<bool> UpdateUserAsync(int id, UpdateUserRequest request);
    Task<bool> DeleteUserAsync(int id);
    Task<PagedResponse<UserDto>> GetAllUsersPagedAsync(UserPagedRequest request);
}
