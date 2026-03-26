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

    public async Task<PagedResponse<TrackingEventDto>> GetByTrackingNumberPagedAsync(
    string trackingNumber, TrackingEventPagedRequest req)
    {
        var query = _context.TrackingEvents
            .Where(t => t.TrackingNumber == trackingNumber)
            .AsQueryable();

        if (!string.IsNullOrEmpty(req.Status))
            query = query.Where(t => t.Status.Contains(req.Status));

        if (req.FromDate.HasValue)
            query = query.Where(t => t.EventTime >= req.FromDate.Value);

        if (req.ToDate.HasValue)
            query = query.Where(t => t.EventTime <= req.ToDate.Value);

        if (!string.IsNullOrEmpty(req.Search))
            query = query.Where(t => t.Location.Contains(req.Search)
                                  || t.Description.Contains(req.Search));

        query = req.SortOrder == "asc"
            ? query.OrderBy(t => t.EventTime)
            : query.OrderByDescending(t => t.EventTime);

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .Select(t => new TrackingEventDto(t.Id, t.TrackingNumber, t.Status, t.Location, t.Description, t.EventTime, t.UpdatedBy))
            .ToListAsync();

        return new PagedResponse<TrackingEventDto>
        {
            Data = items,
            TotalCount = totalCount,
            Page = req.Page,
            PageSize = req.PageSize
        };
    }

    public async Task<PagedResponse<DocumentDto>> GetDocumentsPagedAsync(int shipmentId, DocumentPagedRequest req)
    {
        var query = _context.Documents
            .Where(d => d.ShipmentId == shipmentId)
            .AsQueryable();

        if (!string.IsNullOrEmpty(req.DocumentType) && Enum.TryParse<DocumentType>(req.DocumentType, true, out var dt))
            query = query.Where(d => d.DocumentType == dt);

        if (!string.IsNullOrEmpty(req.Search))
            query = query.Where(d => d.FileName.Contains(req.Search));

        query = req.SortOrder == "asc"
            ? query.OrderBy(d => d.UploadedAt)
            : query.OrderByDescending(d => d.UploadedAt);

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .Select(d => new DocumentDto(d.Id, d.FileName, d.DocumentType.ToString(), d.FileSizeBytes, d.UploadedAt))
            .ToListAsync();

        return new PagedResponse<DocumentDto>
        {
            Data = items,
            TotalCount = totalCount,
            Page = req.Page,
            PageSize = req.PageSize
        };
    }
}
