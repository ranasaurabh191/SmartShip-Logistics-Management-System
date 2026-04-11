using SmartShip.TrackingService.Core.DTOs;
using SmartShip.TrackingService.Core.Interfaces.Persistence;
using SmartShip.TrackingService.Core.Interfaces.Repositories;
using SmartShip.TrackingService.Core.Interfaces.Services;
using SmartShip.TrackingService.Domain.Entities;
using SmartShip.TrackingService.Domain.Enums;

namespace SmartShip.TrackingService.Core.Services;

public class TrackingService : ITrackingService
{
    private readonly ITrackingEventRepository _trackingEventRepository;
    private readonly IDeliveryProofRepository _deliveryProofRepository;
    private readonly IDocumentRepository _documentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _config;
    private readonly ILogger<TrackingService> _logger;

    public TrackingService(
        ITrackingEventRepository trackingEventRepository,
        IDeliveryProofRepository deliveryProofRepository,
        IDocumentRepository documentRepository,
        IUnitOfWork unitOfWork,
        IConfiguration config,
        ILogger<TrackingService> logger)
    {
        _trackingEventRepository = trackingEventRepository;
        _deliveryProofRepository = deliveryProofRepository;
        _documentRepository = documentRepository;
        _unitOfWork = unitOfWork;
        _config = config;
        _logger = logger;
    }

    public async Task<TrackingEventDto> AddEventAsync(AddTrackingEventRequest req, string updatedBy)
    {
        _logger.LogInformation("Adding tracking event for {TrackingNumber} | Status: {Status} | Location: {Location} | By: {UpdatedBy}",
            req.TrackingNumber, req.Status, req.Location, updatedBy);

        try
        {
            var recentDuplicate = await _trackingEventRepository.GetRecentDuplicateAsync(
                req.TrackingNumber,
                req.Status,
                req.Location,
                DateTime.Now.AddMinutes(-1));

            if (recentDuplicate != null)
            {
                _logger.LogWarning("Duplicate event for {TrackingNumber}", req.TrackingNumber);
                throw new InvalidOperationException("Duplicate tracking event submitted recently.");
            }

            var ev = new TrackingEvent
            {
                ShipmentId = req.ShipmentId,
                TrackingNumber = req.TrackingNumber,
                Status = req.Status,
                Location = req.Location,
                Description = req.Description,
                UpdatedBy = updatedBy
            };

            await _trackingEventRepository.AddAsync(ev);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Tracking event added: ID {EventId} for {TrackingNumber} | Status: {Status}",
                ev.Id, ev.TrackingNumber, ev.Status);

            return new TrackingEventDto(
                ev.Id,
                ev.TrackingNumber,
                ev.Status,
                ev.Location,
                ev.Description,
                ev.EventTime.ToString("dd-MMM-yyyy hh:mm tt"),
                ev.UpdatedBy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add tracking event for {TrackingNumber}", req.TrackingNumber);
            throw;
        }
    }

    public async Task<PagedResponse<TrackingEventDto>> GetAllEventsPagedAsync(TrackingEventPagedRequest req)
    {
        _logger.LogInformation("Fetching all tracking events | Page: {Page} | PageSize: {PageSize}",
            req.Page, req.PageSize);

        try
        {
            var result = await _trackingEventRepository.GetAllPagedAsync(req);

            var items = result.Data.Select(t => new TrackingEventDto(
                t.Id,
                t.TrackingNumber,
                t.Status,
                t.Location,
                t.Description,
                t.EventTime.ToString("dd-MMM-yyyy hh:mm tt"),
                t.UpdatedBy)).ToList();

            return new PagedResponse<TrackingEventDto>
            {
                Data = items,
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch all tracking events");
            throw;
        }
    }

    public async Task<PagedResponse<TrackingEventDto>> GetByTrackingNumberPagedAsync(string trackingNumber, TrackingEventPagedRequest req)
    {
        _logger.LogInformation("Fetching tracking timeline for {TrackingNumber} | Page: {Page} | PageSize: {PageSize}",
            trackingNumber, req.Page, req.PageSize);

        try
        {
            var result = await _trackingEventRepository.GetByTrackingNumberPagedAsync(trackingNumber, req);

            var items = result.Data.Select(t => new TrackingEventDto(
                t.Id,
                t.TrackingNumber,
                t.Status,
                t.Location,
                t.Description,
                t.EventTime.ToString("dd-MMM-yyyy hh:mm tt"),
                t.UpdatedBy)).ToList();

            _logger.LogInformation("Fetched {Count} of {Total} events for {TrackingNumber}",
                items.Count, result.TotalCount, trackingNumber);

            return new PagedResponse<TrackingEventDto>
            {
                Data = items,
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch tracking events for {TrackingNumber}", trackingNumber);
            throw;
        }
    }

    public async Task<DeliveryProofDto> GetDeliveryProofAsync(int shipmentId)
    {
        _logger.LogInformation("Fetching delivery proof for Shipment {ShipmentId}", shipmentId);

        var p = await _deliveryProofRepository.GetByShipmentIdAsync(shipmentId);

        if (p == null)
        {
            _logger.LogWarning("Delivery proof not found for Shipment {ShipmentId}", shipmentId);
            throw new KeyNotFoundException($"Delivery proof not found for Shipment {shipmentId}.");
        }

        _logger.LogInformation("Delivery proof found for {TrackingNumber} | Delivered by: {DeliveredBy}",
            p.TrackingNumber, p.DeliveredBy);

        return new DeliveryProofDto(
            p.ShipmentId,
            p.TrackingNumber,
            p.ReceiverName,
            p.SignatureImagePath,
            p.PhotoPath,
            p.Notes,
            p.DeliveredAt.ToString("dd-MMM-yyyy hh:mm tt"),
            p.DeliveredBy);
    }

    public async Task<DeliveryProofDto> AddDeliveryProofAsync(AddDeliveryProofRequest req, string? signaturePath, string? photoPath)
    {
        _logger.LogInformation("Adding delivery proof for {TrackingNumber} | Receiver: {ReceiverName} | By: {DeliveredBy}",
            req.TrackingNumber, req.ReceiverName, req.DeliveredBy);

        try
        {
            var existing = await _deliveryProofRepository.GetByTrackingNumberAsync(req.TrackingNumber);

            if (existing != null)
            {
                _logger.LogWarning("Delivery proof already exists for {TrackingNumber}", req.TrackingNumber);
                throw new InvalidOperationException("Delivery proof already exists for this shipment.");
            }

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

            await _deliveryProofRepository.AddAsync(proof);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Delivery proof saved for {TrackingNumber} | Signature: {HasSignature} | Photo: {HasPhoto}",
                proof.TrackingNumber,
                signaturePath != null ? "Yes" : "No",
                photoPath != null ? "Yes" : "No");

            return new DeliveryProofDto(
                proof.ShipmentId,
                proof.TrackingNumber,
                proof.ReceiverName,
                proof.SignatureImagePath,
                proof.PhotoPath,
                proof.Notes,
                proof.DeliveredAt.ToString("dd-MMM-yyyy hh:mm tt"),
                proof.DeliveredBy);
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
            var existingDoc = await _documentRepository.GetByShipmentIdAndFileNameAsync(shipmentId, file.FileName);

            if (existingDoc != null)
            {
                _logger.LogWarning("Document {FileName} already uploaded for Shipment {ShipmentId}", file.FileName, shipmentId);
                throw new InvalidOperationException($"Document '{file.FileName}' already uploaded for this shipment.");
            }

            var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), _config["FileStorage:UploadPath"] ?? "Uploads");
            Directory.CreateDirectory(uploadPath);

            var fileName = $"{Guid.NewGuid()}_{file.FileName}";
            var filePath = Path.Combine(uploadPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

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

            await _documentRepository.AddAsync(doc);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Document uploaded: ID {DocId} | {FileName} | Type: {DocType} | Shipment: {ShipmentId}",
                doc.Id, doc.FileName, doc.DocumentType, shipmentId);

            return new DocumentDto(
                doc.Id,
                doc.FileName,
                doc.DocumentType.ToString(),
                doc.FileSizeBytes,
                doc.UploadedAt.ToString("dd-MMM-yyyy hh:mm tt"));
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
            var result = await _documentRepository.GetPagedByShipmentIdAsync(shipmentId, req);

            var items = result.Data.Select(d => new DocumentDto(
                d.Id,
                d.FileName,
                d.DocumentType.ToString(),
                d.FileSizeBytes,
                d.UploadedAt.ToString("dd-MMM-yyyy hh:mm tt"))).ToList();

            _logger.LogInformation("Fetched {Count} of {Total} documents for Shipment {ShipmentId}",
                items.Count, result.TotalCount, shipmentId);

            return new PagedResponse<DocumentDto>
            {
                Data = items,
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch documents for Shipment {ShipmentId}", shipmentId);
            throw;
        }
    }
}