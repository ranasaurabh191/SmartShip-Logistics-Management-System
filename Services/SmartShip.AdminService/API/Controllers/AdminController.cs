using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartShip.AdminService.Core.DTOs;
using SmartShip.AdminService.Core.Interfaces.Services;
using System.Security.Claims;

namespace SmartShip.AdminService.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "ADMIN")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _service;
    public AdminController(IAdminService service) => _service = service;

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard() => Ok(await _service.GetDashboardAsync());

    [HttpGet("hubs")]
    public async Task<IActionResult> GetHubs([FromQuery] HubPagedRequest request) => Ok(await _service.GetHubsPagedAsync(request));

    [HttpGet("hubs/{id}")]
    public async Task<IActionResult> GetHub(int id)
    {
        var h = await _service.GetHubByIdAsync(id);
        return h == null ? NotFound() : Ok(h);
    }

    [HttpPost("hubs")]
    public async Task<IActionResult> CreateHub([FromBody] CreateHubRequest req) =>  Ok(await _service.CreateHubAsync(req));

    [HttpPut("hubs/{id}")]
    public async Task<IActionResult> UpdateHub(int id, [FromBody] UpdateHubRequest req)
    {
        await _service.UpdateHubAsync(id, req);
        return Ok("Updated Successfully");
    }

    [HttpDelete("hubs/{id}")]
    public async Task<IActionResult> DeleteHub(int id)
    {
        await _service.DeleteHubAsync(id);
        return Ok(new { message = "Deleted Successfully" });
    }

    [HttpGet("reports")]
    public async Task<IActionResult> GetReports([FromQuery] ReportPagedRequest request) => Ok(await _service.GetReportsPagedAsync(request));

    [HttpPost("reports")]
    public async Task<IActionResult> GenerateReport([FromBody] ReportRequest req)
    {
        var user = User.FindFirstValue(ClaimTypes.Name) ?? "Admin";
        var result = await _service.GenerateReportAsync(req);
        return Ok(result);
    }
}
