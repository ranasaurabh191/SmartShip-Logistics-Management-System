namespace SmartShip.TrackingService.Models;

public enum DocumentType { Invoice, ShippingLabel, Other }

public class Document
{
    public int Id { get; set; }
    public int ShipmentId { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DocumentType DocumentType { get; set; }
    public long FileSizeBytes { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.Now;
    public int UploadedByUserId { get; set; }
}
