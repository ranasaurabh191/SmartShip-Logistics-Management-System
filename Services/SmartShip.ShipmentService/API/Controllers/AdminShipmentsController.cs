using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartShip.ShipmentService.Core.DTOs;
using SmartShip.ShipmentService.Core.Interfaces.Services;

namespace SmartShip.ShipmentService.API.Controllers;

[ApiController]
[Route("api/admin/shipments")]
[Authorize(Roles = "ADMIN")]
public class AdminShipmentsController : ControllerBase
{
    private readonly IShipmentService _service;
    public AdminShipmentsController(IShipmentService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] ShipmentPagedRequest request) =>  Ok(await _service.GetAllPagedAsync(request));

    [HttpPut("status/{id}")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequest request)
    {
        await _service.UpdateStatusAsync(id, request);      
        return Ok(new { message = "Status updated successfully." });
    }

    [HttpPut("resolve/{id}")]
    public async Task<IActionResult> Resolve(int id, [FromBody] UpdateStatusRequest req)
    {
        if (string.IsNullOrEmpty(req.Resolution)) throw new ArgumentException("Resolution text is required.");

        await _service.ResolveExceptionAsync(id, req.Resolution);
        return Ok(new { message = "Exception resolved successfully." });
    }

    [HttpGet("route/{id}")]
    public async Task<IActionResult> GetRoute(int id) => Ok(await _service.GetRouteAsync(id));

    [HttpPut("advance-hub/{id}")]
    public async Task<IActionResult> AdvanceToNextHub(int id) => Ok(await _service.AdvanceToNextHubAsync(id));
}
