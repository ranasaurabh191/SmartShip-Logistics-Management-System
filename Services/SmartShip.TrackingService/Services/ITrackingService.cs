using SmartShip.TrackingService.DTOs;

namespace SmartShip.TrackingService.Services
{
    public interface ITrackingService
    {
        Task<TrackingEventDto> AddEventAsync(AddTrackingEventRequest req, string updatedBy);
        Task<IEnumerable<TrackingEventDto>> GetByTrackingNumberAsync(string trackingNumber);
        Task<DeliveryProofDto?> GetDeliveryProofAsync(int shipmentId);
        Task<DeliveryProofDto> AddDeliveryProofAsync(AddDeliveryProofRequest req, string? signaturePath, string? photoPath);
        Task<DocumentDto> UploadDocumentAsync(int shipmentId, string trackingNumber, IFormFile file, string docType, int userId);
        Task<IEnumerable<DocumentDto>> GetDocumentsAsync(int shipmentId);
    }
}
