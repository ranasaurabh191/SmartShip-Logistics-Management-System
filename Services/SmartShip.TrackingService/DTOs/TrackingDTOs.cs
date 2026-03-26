using SmartShip.TrackingService.Models;

namespace SmartShip.TrackingService.DTOs;

public record AddTrackingEventRequest(int ShipmentId, string TrackingNumber, string Status, string Location, string Description);
public record TrackingEventDto(int Id, string TrackingNumber, string Status, string Location, string Description, DateTime EventTime, string UpdatedBy);
public record DocumentDto(int Id, string FileName, string DocumentType, long FileSizeBytes, DateTime UploadedAt);
public record DeliveryProofDto(int ShipmentId, string TrackingNumber, string ReceiverName, string? SignatureImagePath, string? PhotoPath, string Notes, DateTime DeliveredAt, string DeliveredBy);
public record AddDeliveryProofRequest(int ShipmentId, string TrackingNumber, string ReceiverName, string Notes, string DeliveredBy);
