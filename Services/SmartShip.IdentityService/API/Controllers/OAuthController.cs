using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using SmartShip.IdentityService.Core.Interfaces.Services;
using System.Security.Claims;

namespace SmartShip.IdentityService.API.Controllers;

[ApiController]
[Route("auth/oauth")]
public class OAuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IConfiguration _config;
    private readonly ILogger<OAuthController> _logger;

    public OAuthController(IAuthService authService, IConfiguration config, ILogger<OAuthController> logger)
    {
        _authService = authService;
        _config = config;
        _logger = logger;
    }

    [HttpGet("google")]
    public IActionResult GoogleLogin()
    {
        var props = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(GoogleCallback))
        };
        return Challenge(props, "Google");
    }

    [HttpGet("google/callback")]
    public async Task<IActionResult> GoogleCallback()
    {
        return await HandleOAuthCallback("Google");
    }

    [HttpGet("github")]
    public IActionResult GitHubLogin()
    {
        var props = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(GitHubCallback))
        };
        return Challenge(props, "GitHub");
    }

    [HttpGet("github/callback")]
    public async Task<IActionResult> GitHubCallback()
    {
        return await HandleOAuthCallback("GitHub");
    }

    private async Task<IActionResult> HandleOAuthCallback(string provider)
    {
        var result = await HttpContext.AuthenticateAsync("ExternalCookie");

        if (!result.Succeeded)
        {
            _logger.LogWarning("{Provider} OAuth callback failed.", provider);
            var frontendError = _config["OAuth:FrontendCallbackUrl"] + "?error=oauth_failed";
            return Redirect(frontendError);
        }

        var email = result.Principal!.FindFirst(ClaimTypes.Email)?.Value;
        var name = result.Principal!.FindFirst(ClaimTypes.Name)?.Value
                    ?? result.Principal!.FindFirst("urn:github:name")?.Value
                    ?? "User";

        if (string.IsNullOrWhiteSpace(email))
        {
            _logger.LogWarning("{Provider} OAuth: email claim missing.", provider);
            return Redirect(_config["OAuth:FrontendCallbackUrl"] + "?error=no_email");
        }

        _logger.LogInformation("{Provider} OAuth callback for email: {Email}", provider, email);

        var (token, userId, role) = await _authService.FindOrCreateOAuthUserAsync(email, name!, provider);

        await HttpContext.SignOutAsync("ExternalCookie");

        var frontendUrl = _config["OAuth:FrontendCallbackUrl"] + $"?token={token}&userId={userId}&role={role}&name={Uri.EscapeDataString(name!)}&email={Uri.EscapeDataString(email!)}";

        return Redirect(frontendUrl);
    }
}