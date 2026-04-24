using SmartShip.TrackingService.Core.DTOs;
using SmartShip.TrackingService.Domain.Entities;

namespace SmartShip.TrackingService.Core.Interfaces.Repositories;

public interface IDocumentRepository
{
    Task<Document?> GetByShipmentIdAndFileNameAsync(int shipmentId, string fileName);
    Task AddAsync(Document document);
    Task<PagedResponse<Document>> GetPagedByShipmentIdAsync(int shipmentId, DocumentPagedRequest request);
    Task<Document?> GetByIdAsync(int id);

}