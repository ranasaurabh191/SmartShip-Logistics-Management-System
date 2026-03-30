using SmartShip.TrackingService.Models;

namespace SmartShip.TrackingService.DTOs;

public record AddTrackingEventRequest(int ShipmentId, string TrackingNumber, string Status, string Location, string Description);
public record TrackingEventDto(int Id, string TrackingNumber, string Status, string Location, string Description, string EventTime, string UpdatedBy);
public record DocumentDto(int Id, string FileName, string DocumentType, long FileSizeBytes, string UploadedAt);
public record DeliveryProofDto(int ShipmentId, string TrackingNumber, string ReceiverName, string? SignatureImagePath, string? PhotoPath, string Notes, string DeliveredAt, string DeliveredBy);
public record AddDeliveryProofRequest(int ShipmentId, string TrackingNumber, string ReceiverName, string Notes, string DeliveredBy);

public class TrackingEventPagedRequest : PagedRequest
{
    public string? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

public class DocumentPagedRequest : PagedRequest
{
    public string? DocumentType { get; set; }
}