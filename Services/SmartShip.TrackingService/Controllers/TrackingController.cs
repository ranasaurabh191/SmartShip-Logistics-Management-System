using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartShip.TrackingService.DTOs;
using System.Security.Claims;

namespace SmartShip.TrackingService.Controllers;

[ApiController]
[Route("api/tracking")]
[Authorize]
public class TrackingController : ControllerBase
{
    private readonly ITrackingService _service;
    public TrackingController(ITrackingService service) => _service = service;

    [HttpGet("{trackingNumber}")]
    public async Task<IActionResult> GetTimeline( string trackingNumber, [FromQuery] TrackingEventPagedRequest request) =>
        Ok(await _service.GetByTrackingNumberPagedAsync(trackingNumber, request));

    [HttpPost("events")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> AddEvent([FromBody] AddTrackingEventRequest req)
    {
        var updatedBy = User.FindFirstValue(ClaimTypes.Name) ?? "System";
        var (result, error) = await _service.AddEventAsync(req, updatedBy);
        if (error != null) return Conflict(new { message = error });
        return Ok(result);
    }

    [HttpGet("delivery/{shipmentId}")]
    public async Task<IActionResult> GetDeliveryProof(int shipmentId)
    {
        var result = await _service.GetDeliveryProofAsync(shipmentId);
        return result == null ? NotFound(new { message = "Delivery proof not found." }) : Ok(result);
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

        var (result, error) = await _service.AddDeliveryProofAsync(req, sigPath, photoPath);
        if (error != null) return Conflict(new { message = error });
        return Ok(result);
    }

    [HttpPost("documents/upload")]
    public async Task<IActionResult> UploadDocument(
        [FromForm] int shipmentId, [FromForm] string trackingNumber,
        [FromForm] string documentType, IFormFile file)
    {
        if (file.Length > 10 * 1024 * 1024) return BadRequest("File must be under 10MB");
        var allowed = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
        var ext = Path.GetExtension(file.FileName).ToLower();
        if (!allowed.Contains(ext)) return BadRequest("Only PDF, JPG, PNG allowed");

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var (result, error) = await _service.UploadDocumentAsync(shipmentId, trackingNumber, file, documentType, userId);
        if (error != null) return Conflict(new { message = error });
        return Ok(result);
    }

    [HttpGet("documents/{shipmentId}")]
    public async Task<IActionResult> GetDocuments(
        int shipmentId, [FromQuery] DocumentPagedRequest request) =>
        Ok(await _service.GetDocumentsPagedAsync(shipmentId, request));
}
