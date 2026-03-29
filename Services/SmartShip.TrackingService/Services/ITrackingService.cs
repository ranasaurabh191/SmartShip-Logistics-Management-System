using SmartShip.TrackingService.DTOs;

public interface ITrackingService
{
    Task<(TrackingEventDto? Data, string? Error)> AddEventAsync(AddTrackingEventRequest req, string updatedBy);
    Task<PagedResponse<TrackingEventDto>> GetByTrackingNumberPagedAsync(string trackingNumber, TrackingEventPagedRequest request);
    Task<DeliveryProofDto?> GetDeliveryProofAsync(int shipmentId);
    Task<(DeliveryProofDto? Data, string? Error)> AddDeliveryProofAsync(AddDeliveryProofRequest req, string? sigPath, string? photoPath);
    Task<PagedResponse<DocumentDto>> GetDocumentsPagedAsync(int shipmentId, DocumentPagedRequest request);
    Task<(DocumentDto? Data, string? Error)> UploadDocumentAsync(int shipmentId, string trackingNumber, IFormFile file, string docType, int userId);

}