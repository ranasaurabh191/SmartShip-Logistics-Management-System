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

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequest req)
    {
        var result = await _service.UpdateStatusAsync(id, req);
        return result ? Ok("Updated Sucessfully") : BadRequest("Invalid status or shipment not found");
    }

    [HttpPut("{id}/resolve")]
    public async Task<IActionResult> Resolve(int id, [FromBody] UpdateStatusRequest req)
    {
        var result = await _service.ResolveExceptionAsync(id, req.Status);
        return result ? NoContent() : NotFound();
    }
}
