using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartShip.ShipmentService.Data;
using SmartShip.ShipmentService.DTOs;
using SmartShip.ShipmentService.Filters;
using SmartShip.ShipmentService.Models;
using SmartShip.ShipmentService.Services;
using System.Security.Claims;

namespace SmartShip.ShipmentService.Controllers;

[ApiController]
[Route("api/shipments")]
[Authorize]
public class ShipmentsController : ControllerBase
{
    private readonly IShipmentService _service;
    private readonly ShipmentDbContext _context;
    private readonly IConfiguration _config;
    private readonly ILogger<ShipmentsController> _logger;

    public ShipmentsController(IShipmentService service, ShipmentDbContext context, IConfiguration config, ILogger<ShipmentsController> logger)
    {
        _service = service;
        _context = context; 
        _config = config;
        _logger = logger;
    }
    
    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost]
    [Authorize(Roles = "CUSTOMER")]
    public async Task<IActionResult> Create([FromBody] CreateShipmentRequest request)
    {
        var result = await _service.CreateAsync(request, GetUserId());
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("my")]
    [Authorize(Roles = "CUSTOMER")]
    public async Task<IActionResult> GetMine([FromQuery] PagedRequest request) =>
    Ok(await _service.GetMyShipmentsPagedAsync(GetUserId(), request));

    [HttpGet("{id}")]
    [Authorize(Roles = "CUSTOMER")]
    public async Task<IActionResult> GetById(int id) =>  Ok(await _service.GetByIdAsync(id));

    [HttpPatch("pickup/{id}")]
    [Authorize(Roles = "CUSTOMER")]
    public async Task<IActionResult> SchedulePickup(int id, [FromBody] SchedulePickupRequest request)
    {
        await _service.SchedulePickupAsync(id, request);        
        return Ok(new { message = "Pickup scheduled successfully." });
    }

    [HttpGet("rate")]
    public async Task<IActionResult> GetRate([FromQuery] double weight, [FromQuery] string type)
    {
        if (!Enum.TryParse<ShipmentType>(type, true, out var shipType)) return BadRequest("Invalid type");
        var rate = await _service.CalculateRateAsync(weight, shipType);
        return Ok(new { rate });
    }
    [HttpPatch("{id}/cancel")]
    public async Task<IActionResult> CancelShipment(int id, [FromBody] CancelShipmentRequest request)
    {
        var userIdClaim = User.FindFirst("userId")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var customerId))
            return Unauthorized("Invalid token.");

        await _service.CancelByCustomerAsync(id, customerId, request.Reason);
        return Ok(new { message = "Shipment cancelled successfully." });

    }
    [HttpGet("internal/{id}")]
    [InternalApiKey]  
    public async Task<IActionResult> GetByIdInternal(int id) =>  Ok(await _service.GetByIdAsync(id));

    [HttpGet("{shipmentId}/saga-correlation")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSagaCorrelation(int shipmentId,
    [FromHeader(Name = "X-Internal-Key")] string? internalKey)
    {
        if (internalKey != _config["InternalApi:Key"])
            return Unauthorized();

        var saga = await _context.ShipmentOrderSagas
            .FirstOrDefaultAsync(s => s.ShipmentId == shipmentId);

        if (saga == null)
            return NotFound();

        _logger.LogInformation("Returning CorrelationId {CorrelationId} for ShipmentId {ShipmentId}",
            saga.CorrelationId, shipmentId);

        return Ok(new { correlationId = saga.CorrelationId });
    }

}
    