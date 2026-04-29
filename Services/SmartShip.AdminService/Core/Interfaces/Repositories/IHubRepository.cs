using SmartShip.AdminService.Core.DTOs;
using SmartShip.AdminService.Domain.Entities;

namespace SmartShip.AdminService.Core.Interfaces.Repositories;

public interface IHubRepository
{
    Task<PagedResponse<Hub>> GetPagedAsync(HubPagedRequest request);
    Task<Hub?> GetByIdAsync(int id);
    Task<Hub> AddAsync(Hub hub);
    Task UpdateAsync(Hub hub);
    Task DeleteAsync(Hub hub);
    Task<List<Hub>> GetAllActiveAsync();
}

