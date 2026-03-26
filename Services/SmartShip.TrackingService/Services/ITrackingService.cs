using SmartShip.TrackingService.DTOs;

public interface ITrackingService
{
    Task<TrackingEventDto> AddEventAsync(AddTrackingEventRequest req, string updatedBy);
    Task<PagedResponse<TrackingEventDto>> GetByTrackingNumberPagedAsync(string trackingNumber, TrackingEventPagedRequest request);
    Task<DeliveryProofDto?> GetDeliveryProofAsync(int shipmentId);
    Task<DeliveryProofDto> AddDeliveryProofAsync(AddDeliveryProofRequest req, string? signaturePath, string? photoPath);
    Task<PagedResponse<DocumentDto>> GetDocumentsPagedAsync(int shipmentId, DocumentPagedRequest request);
    Task<DocumentDto> UploadDocumentAsync(int shipmentId, string trackingNumber, IFormFile file, string docType, int userId);

}