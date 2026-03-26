using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartShip.TrackingService.Data;
using SmartShip.TrackingService.DTOs;
using SmartShip.TrackingService.Models;

namespace SmartShip.TrackingService.Services;

public class TrackingService : ITrackingService
{
    private readonly TrackingDbContext _context;
    private readonly IConfiguration _config;
    private readonly ILogger<TrackingService> _logger;

    public TrackingService(TrackingDbContext context, IConfiguration config, ILogger<TrackingService> logger)
    {
        _context = context;
        _config = config;
        _logger = logger;
    }

    public async Task<TrackingEventDto> AddEventAsync(AddTrackingEventRequest req, string updatedBy)
    {
        _logger.LogInformation("Adding tracking event for {TrackingNumber} | Status: {Status} | Location: {Location} | By: {UpdatedBy}",
            req.TrackingNumber, req.Status, req.Location, updatedBy);

        try
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

            _logger.LogInformation("Tracking event added: ID {EventId} for {TrackingNumber} | Status: {Status}",
                ev.Id, ev.TrackingNumber, ev.Status);

            return new TrackingEventDto(ev.Id, ev.TrackingNumber, ev.Status, ev.Location, ev.Description, ev.EventTime, ev.UpdatedBy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add tracking event for {TrackingNumber}", req.TrackingNumber);
            throw;
        }
    }

    public async Task<PagedResponse<TrackingEventDto>> GetByTrackingNumberPagedAsync(
        string trackingNumber, TrackingEventPagedRequest req)
    {
        _logger.LogInformation("Fetching tracking timeline for {TrackingNumber} | Page: {Page} | PageSize: {PageSize}",
            trackingNumber, req.Page, req.PageSize);

        try
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

            _logger.LogInformation("Fetched {Count} of {Total} events for {TrackingNumber}",
                items.Count, totalCount, trackingNumber);

            return new PagedResponse<TrackingEventDto>
            {
                Data = items,
                TotalCount = totalCount,
                Page = req.Page,
                PageSize = req.PageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch tracking events for {TrackingNumber}", trackingNumber);
            throw;
        }
    }

    public async Task<DeliveryProofDto?> GetDeliveryProofAsync(int shipmentId)
    {
        _logger.LogInformation("Fetching delivery proof for Shipment {ShipmentId}", shipmentId);

        var p = await _context.DeliveryProofs.FirstOrDefaultAsync(d => d.ShipmentId == shipmentId);

        if (p == null)
        {
            _logger.LogWarning("Delivery proof not found for Shipment {ShipmentId}", shipmentId);
            return null;
        }

        _logger.LogInformation("Delivery proof found for {TrackingNumber} | Delivered by: {DeliveredBy}",
            p.TrackingNumber, p.DeliveredBy);

        return new DeliveryProofDto(p.ShipmentId, p.TrackingNumber, p.ReceiverName,
            p.SignatureImagePath, p.PhotoPath, p.Notes, p.DeliveredAt, p.DeliveredBy);
    }

    public async Task<DeliveryProofDto> AddDeliveryProofAsync(AddDeliveryProofRequest req, string? signaturePath, string? photoPath)
    {
        _logger.LogInformation("Adding delivery proof for {TrackingNumber} | Receiver: {ReceiverName} | By: {DeliveredBy}",
            req.TrackingNumber, req.ReceiverName, req.DeliveredBy);

        try
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

            _logger.LogInformation("Delivery proof saved for {TrackingNumber} | Signature: {HasSignature} | Photo: {HasPhoto}",
                proof.TrackingNumber,
                signaturePath != null ? "Yes" : "No",
                photoPath != null ? "Yes" : "No");

            return new DeliveryProofDto(proof.ShipmentId, proof.TrackingNumber, proof.ReceiverName,
                proof.SignatureImagePath, proof.PhotoPath, proof.Notes, proof.DeliveredAt, proof.DeliveredBy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add delivery proof for {TrackingNumber}", req.TrackingNumber);
            throw;
        }
    }

    public async Task<DocumentDto> UploadDocumentAsync(int shipmentId, string trackingNumber, IFormFile file, string docType, int userId)
    {
        _logger.LogInformation("Uploading document for Shipment {ShipmentId} | File: {FileName} | Type: {DocType} | User: {UserId}",
            shipmentId, file.FileName, docType, userId);

        try
        {
            var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), _config["FileStorage:UploadPath"] ?? "Uploads");
            Directory.CreateDirectory(uploadPath);

            var fileName = $"{Guid.NewGuid()}_{file.FileName}";
            var filePath = Path.Combine(uploadPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
                await file.CopyToAsync(stream);

            _logger.LogInformation("File saved to disk: {FilePath} | Size: {Size} bytes", filePath, file.Length);

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

            _logger.LogInformation("Document uploaded: ID {DocId} | {FileName} | Type: {DocType} | Shipment: {ShipmentId}",
                doc.Id, doc.FileName, doc.DocumentType, shipmentId);

            return new DocumentDto(doc.Id, doc.FileName, doc.DocumentType.ToString(), doc.FileSizeBytes, doc.UploadedAt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload document for Shipment {ShipmentId} | File: {FileName}",
                shipmentId, file.FileName);
            throw;
        }
    }

    public async Task<PagedResponse<DocumentDto>> GetDocumentsPagedAsync(int shipmentId, DocumentPagedRequest req)
    {
        _logger.LogInformation("Fetching documents for Shipment {ShipmentId} | Page: {Page} | Type: {DocType}",
            shipmentId, req.Page, req.DocumentType ?? "All");

        try
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

            _logger.LogInformation("Fetched {Count} of {Total} documents for Shipment {ShipmentId}",
                items.Count, totalCount, shipmentId);

            return new PagedResponse<DocumentDto>
            {
                Data = items,
                TotalCount = totalCount,
                Page = req.Page,
                PageSize = req.PageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch documents for Shipment {ShipmentId}", shipmentId);
            throw;
        }
    }
}