using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartShip.ShipmentService.DTOs;
using SmartShip.ShipmentService.Services;

namespace SmartShip.ShipmentService.Controllers;

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
}
