using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartShip.ShipmentService.DTOs;
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
    public ShipmentsController(IShipmentService service) => _service = service;

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
    public async Task<IActionResult> GetById(int id)
    {
        var s = await _service.GetByIdAsync(id);
        return s == null ? NotFound() : Ok(s);
    }

    [HttpPatch("{id}/pickup")]
    [Authorize(Roles = "CUSTOMER")]
    public async Task<IActionResult> SchedulePickup(int id, [FromBody] SchedulePickupRequest req)
    {
        var result = await _service.SchedulePickupAsync(id, req);
        return result ? Ok("Pickup Scheduled Successfully") : NotFound();
    }

    [HttpGet("rate")]
    public async Task<IActionResult> GetRate([FromQuery] double weight, [FromQuery] string type)
    {
        if (!Enum.TryParse<ShipmentType>(type, true, out var shipType)) return BadRequest("Invalid type");
        var rate = await _service.CalculateRateAsync(weight, shipType);
        return Ok(new { rate });
    }
}
    