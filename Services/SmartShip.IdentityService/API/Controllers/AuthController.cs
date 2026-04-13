using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartShip.IdentityService.Core.DTOs;
using SmartShip.IdentityService.Core.Interfaces.Services;
namespace SmartShip.IdentityService.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IValidator<SignupOtpRequest> _signupOtpValidator;
    private readonly IValidator<VerifyOtpRequest> _verifyOtpValidator;

    public AuthController(
        IAuthService authService,
        IValidator<SignupOtpRequest> signupOtpValidator,
        IValidator<VerifyOtpRequest> verifyOtpValidator)
    {
        _authService = authService;
        _signupOtpValidator = signupOtpValidator;
        _verifyOtpValidator = verifyOtpValidator;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        return Ok(result);
    }

    [HttpPost("signup/request-otp")]
    public async Task<IActionResult> RequestSignupOtp([FromBody] SignupOtpRequest request)
    {
        await _signupOtpValidator.ValidateAndThrowAsync(request);
        var result = await _authService.RequestSignupOtpAsync(request);
        return Ok(result);
    }

    [HttpPost("signup/verify-otp")]
    public async Task<IActionResult> VerifySignupOtp([FromBody] VerifyOtpRequest request)
    {
        await _verifyOtpValidator.ValidateAndThrowAsync(request);
        var result = await _authService.VerifySignupOtpAsync(request);
        return Ok(result);
    }

    [HttpPost("debug-login")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DebugLogin([FromBody] LoginRequest request) =>
        Ok(await _authService.DebugLoginAsync(request));

    [HttpGet("fix-admin")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> FixAdmin() =>
        Ok(await _authService.FixAdminAsync());
}