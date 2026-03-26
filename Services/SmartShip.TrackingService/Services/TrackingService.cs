using Microsoft.EntityFrameworkCore;
using SmartShip.TrackingService.Data;
using SmartShip.TrackingService.DTOs;
using SmartShip.TrackingService.Models;

namespace SmartShip.TrackingService.Services;

public class TrackingService : ITrackingService
{
    private readonly TrackingDbContext _context;
    private readonly IConfiguration _config;

    public TrackingService(TrackingDbContext context, IConfiguration config)
    { _context = context; _config = config; }

    public async Task<TrackingEventDto> AddEventAsync(AddTrackingEventRequest req, string updatedBy)
    {
        var ev = new TrackingEvent
        {
            ShipmentId = req.ShipmentId,
            TrackingNumber = req.TrackingNumber,
            Status = req.Status,
            Location = req.Location,
            Description = req.Description,
            UpdatedBy = updatedBy
        };
        _context.TrackingEvents.Add(ev);
        await _context.SaveChangesAsync();
        return new TrackingEventDto(ev.Id, ev.TrackingNumber, ev.Status, ev.Location, ev.Description, ev.EventTime, ev.UpdatedBy);
    }

    public async Task<IEnumerable<TrackingEventDto>> GetByTrackingNumberAsync(string trackingNumber)
    {
        return await _context.TrackingEvents
            .Where(t => t.TrackingNumber == trackingNumber)
            .OrderByDescending(t => t.EventTime)
            .Select(t => new TrackingEventDto(t.Id, t.TrackingNumber, t.Status, t.Location, t.Description, t.EventTime, t.UpdatedBy))
            .ToListAsync();
    }

    public async Task<DeliveryProofDto?> GetDeliveryProofAsync(int shipmentId)
    {
        var p = await _context.DeliveryProofs.FirstOrDefaultAsync(d => d.ShipmentId == shipmentId);
        return p == null ? null : new DeliveryProofDto(p.ShipmentId, p.TrackingNumber, p.ReceiverName, p.SignatureImagePath, p.PhotoPath, p.Notes, p.DeliveredAt, p.DeliveredBy);
    }

    public async Task<DeliveryProofDto> AddDeliveryProofAsync(AddDeliveryProofRequest req, string? signaturePath, string? photoPath)
    {
        var proof = new DeliveryProof
        {
            ShipmentId = req.ShipmentId,
            TrackingNumber = req.TrackingNumber,
            ReceiverName = req.ReceiverName,
            Notes = req.Notes,
            DeliveredBy = req.DeliveredBy,
            SignatureImagePath = signaturePath,
            PhotoPath = photoPath
        };
        _context.DeliveryProofs.Add(proof);
        await _context.SaveChangesAsync();
        return new DeliveryProofDto(proof.ShipmentId, proof.TrackingNumber, proof.ReceiverName, proof.SignatureImagePath, proof.PhotoPath, proof.Notes, proof.DeliveredAt, proof.DeliveredBy);
    }

    public async Task<DocumentDto> UploadDocumentAsync(int shipmentId, string trackingNumber, IFormFile file, string docType, int userId)
    {
        var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), _config["FileStorage:UploadPath"] ?? "Uploads");
        Directory.CreateDirectory(uploadPath);
        var fileName = $"{Guid.NewGuid()}_{file.FileName}";
        var filePath = Path.Combine(uploadPath, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
            await file.CopyToAsync(stream);

        Enum.TryParse<DocumentType>(docType, true, out var dt);
        var doc = new Document
        {
            ShipmentId = shipmentId,
            TrackingNumber = trackingNumber,
            FileName = file.FileName,
            FilePath = filePath,
            DocumentType = dt,
            FileSizeBytes = file.Length,
            UploadedByUserId = userId
        };
        _context.Documents.Add(doc);
        await _context.SaveChangesAsync();
        return new DocumentDto(doc.Id, doc.FileName, doc.DocumentType.ToString(), doc.FileSizeBytes, doc.UploadedAt);
    }

    public async Task<IEnumerable<DocumentDto>> GetDocumentsAsync(int shipmentId) =>
        await _context.Documents.Where(d => d.ShipmentId == shipmentId)
            .Select(d => new DocumentDto(d.Id, d.FileName, d.DocumentType.ToString(), d.FileSizeBytes, d.UploadedAt))
            .ToListAsync();
}
