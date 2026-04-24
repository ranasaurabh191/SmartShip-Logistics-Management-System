using SmartShip.TrackingService.Core.DTOs;
using SmartShip.TrackingService.Domain.Entities;
namespace SmartShip.TrackingService.Core.Interfaces.Services;

public interface ITrackingService
{
    Task<TrackingEventDto> AddEventAsync(AddTrackingEventRequest req, string updatedBy);
    Task<PagedResponse<TrackingEventDto>> GetByTrackingNumberPagedAsync(string trackingNumber, TrackingEventPagedRequest request);
    Task<PagedResponse<TrackingEventDto>> GetAllEventsPagedAsync(TrackingEventPagedRequest req);
    Task<DeliveryProofDto> GetDeliveryProofAsync(int shipmentId);
    Task<DeliveryProofDto> AddDeliveryProofAsync(AddDeliveryProofRequest req, string? sigPath, string? photoPath);
    Task<PagedResponse<DocumentDto>> GetDocumentsPagedAsync(int shipmentId, DocumentPagedRequest request);
    Task<DocumentDto> UploadDocumentAsync(int shipmentId, string trackingNumber, IFormFile file, string docType, int userId);
    Task<Document?> GetDocumentByIdAsync(int documentId);
    Task<DeliveryProof?> GetDeliveryProofEntityAsync(int shipmentId);

}