using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartShip.NotificationService.Core.DTOs;
using SmartShip.NotificationService.Core.Interfaces.Services;
using System.Security.Claims;

namespace SmartShip.NotificationService.API.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _service;
    public NotificationController(INotificationService service) => _service = service;

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> GetAll([FromQuery] NotificationPagedRequest request) =>
        Ok(await _service.GetAllPagedAsync(request));

    [HttpGet("my")]
    [Authorize(Roles = "CUSTOMER")]
    public async Task<IActionResult> GetMine([FromQuery] NotificationPagedRequest request) =>
        Ok(await _service.GetMyNotificationsAsync(GetUserId(), request));
}