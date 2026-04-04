using SmartShip.IdentityService.Core.DTOs;
using SmartShip.IdentityService.Domain.Entities;

namespace SmartShip.IdentityService.Core.Interfaces.Repositories;

public interface IUserRepository
{
    Task<bool> ExistsByEmailAsync(string email);
    Task<bool> ExistsActiveByIdAsync(int userId);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByEmailForAdminAsync(string email);
    Task AddAsync(User user);
    void Update(User user);
    void Delete(User user);
    Task<PagedResponse<User>> GetPagedAsync(UserPagedRequest request);
}