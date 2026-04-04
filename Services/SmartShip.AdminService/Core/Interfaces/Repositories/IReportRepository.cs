using SmartShip.AdminService.Core.DTOs;
using SmartShip.AdminService.Domain.Entities;

namespace SmartShip.AdminService.Core.Interfaces.Repositories;

public interface IReportRepository
{
    Task<Report> AddAsync(Report report);
    Task<PagedResponse<Report>> GetPagedAsync(ReportPagedRequest request);
}