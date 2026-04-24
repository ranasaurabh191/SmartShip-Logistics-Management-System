using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartShip.TrackingService.Core.DTOs;
using SmartShip.TrackingService.Core.Interfaces.Services;
using System.Security.Claims;

namespace SmartShip.TrackingService.API.Controllers;

[ApiController]
[Route("api/tracking")]
[Authorize]
public class TrackingController : ControllerBase
{
    private readonly ITrackingService _service;
    public TrackingController(ITrackingService service) => _service = service;
    
    [HttpGet("events")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> GetAllEvents([FromQuery] TrackingEventPagedRequest request) =>
    Ok(await _service.GetAllEventsPagedAsync(request));

    [HttpGet("{trackingNumber}")]
    public async Task<IActionResult> GetTimeline( string trackingNumber, [FromQuery] TrackingEventPagedRequest request) =>
        Ok(await _service.GetByTrackingNumberPagedAsync(trackingNumber, request));

    [HttpPost("events")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> AddEvent([FromBody] AddTrackingEventRequest req)
    {
        var updatedBy = User.FindFirstValue(ClaimTypes.Name) ?? "System";
        var result = await _service.AddEventAsync(req, updatedBy);
        return Ok(result);
    }

    [HttpGet("delivery-proof/{shipmentId}")]
    [Authorize]
    public async Task<IActionResult> GetDeliveryProof(int shipmentId) => Ok(await _service.GetDeliveryProofAsync(shipmentId));
    [HttpGet("delivery-proof/{shipmentId}/signature")]
    [Authorize]
    public async Task<IActionResult> GetSignatureImage(int shipmentId)
    {
        var proof = await _service.GetDeliveryProofEntityAsync(shipmentId);
        if (proof == null || proof.SignatureImagePath == null) return NotFound();
        if (!System.IO.File.Exists(proof.SignatureImagePath)) return NotFound();

        var ext = Path.GetExtension(proof.SignatureImagePath).ToLower();
        var mime = ext == ".png" ? "image/png" : "image/jpeg";
        return File(await System.IO.File.ReadAllBytesAsync(proof.SignatureImagePath), mime);
    }

    [HttpGet("delivery-proof/{shipmentId}/photo")]
    [Authorize]
    public async Task<IActionResult> GetDeliveryPhoto(int shipmentId)
    {
        var proof = await _service.GetDeliveryProofEntityAsync(shipmentId);
        if (proof == null || proof.PhotoPath == null) return NotFound();
        if (!System.IO.File.Exists(proof.PhotoPath)) return NotFound();

        var ext = Path.GetExtension(proof.PhotoPath).ToLower();
        var mime = ext == ".png" ? "image/png" : "image/jpeg";
        return File(await System.IO.File.ReadAllBytesAsync(proof.PhotoPath), mime);
    }

    [HttpPost("delivery-proof")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> AddDeliveryProof([FromForm] AddDeliveryProofRequest req,
        IFormFile? signature, IFormFile? photo)
    {
        var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
        Directory.CreateDirectory(uploadPath);
        string? sigPath = null, photoPath = null;

        if (signature != null)
        {
            sigPath = Path.Combine(uploadPath, $"sig_{Guid.NewGuid()}_{signature.FileName}");
            using var s = new FileStream(sigPath, FileMode.Create);
            await signature.CopyToAsync(s);
        }
        if (photo != null)
        {
            photoPath = Path.Combine(uploadPath, $"photo_{Guid.NewGuid()}_{photo.FileName}");
            using var s = new FileStream(photoPath, FileMode.Create);
            await photo.CopyToAsync(s);
        }

        var result = await _service.AddDeliveryProofAsync(req, sigPath, photoPath);
        return Ok(result);
    }

    [HttpPost("documents/upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadDocument(
        [FromForm] int shipmentId, [FromForm] string trackingNumber,
        [FromForm] string documentType, IFormFile? file)
        {
            if (file == null) throw new ArgumentException("File is required.");

            if (file.Length > 10 * 1024 * 1024) throw new ArgumentException("File must be under 10MB.");  

            var allowed = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
            var ext = Path.GetExtension(file.FileName).ToLower();
            if (!allowed.Contains(ext)) throw new ArgumentException("Only PDF, JPG, PNG allowed.");  

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _service.UploadDocumentAsync(shipmentId, trackingNumber, file, documentType, userId);
            return Ok(result); 
        }

    [HttpGet("documents/{shipmentId}")]
    [Authorize]
    public async Task<IActionResult> GetDocuments(
        int shipmentId, [FromQuery] DocumentPagedRequest request) => Ok(await _service.GetDocumentsPagedAsync(shipmentId, request));

    [HttpGet("document/file/{documentId}")]
    public async Task<IActionResult> DownloadDocument(int documentId)
    {
        var doc = await _service.GetDocumentByIdAsync(documentId);
        if (doc == null) return NotFound("Document not found.");

        if (!System.IO.File.Exists(doc.FilePath))
            return NotFound("File not found on disk.");

        var ext = Path.GetExtension(doc.FileName).ToLower();
        var contentType = ext switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            _ => "application/octet-stream"
        };

        var fileBytes = await System.IO.File.ReadAllBytesAsync(doc.FilePath);
        return File(fileBytes, contentType, doc.FileName);
    }
}

