using SmartShip.TrackingService.Domain.Enums;

namespace SmartShip.TrackingService.Domain.Entities;

public class Document
{
    public int Id { get; set; }
    public int ShipmentId { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DocumentType DocumentType { get; set; }
    public long FileSizeBytes { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public int UploadedByUserId { get; set; }
}
