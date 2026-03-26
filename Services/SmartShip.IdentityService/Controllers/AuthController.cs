using Microsoft.AspNetCore.Mvc;
using SmartShip.IdentityService.DTOs;
using SmartShip.IdentityService.Services;

namespace SmartShip.IdentityService.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    public AuthController(IAuthService authService) => _authService = authService;

    [HttpPost("signup")]
    public async Task<IActionResult> Signup([FromBody] SignupRequest request)
    {
        var result = await _authService.SignupAsync(request);
        if (result == null) return Conflict(new { message = "Email already exists." });
        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        if (result == null) return Unauthorized(new { message = "Invalid credentials." });
        return Ok(result);
    }

    // ✅ Temp debug - DELETE after fixing
    [HttpPost("debug-login")]
    public async Task<IActionResult> DebugLogin([FromBody] LoginRequest request) =>
        Ok(await _authService.DebugLoginAsync(request));

    // ✅ Temp fix - DELETE after fixing
    [HttpGet("fix-admin")]
    public async Task<IActionResult> FixAdmin() =>
        Ok(await _authService.FixAdminAsync());
}