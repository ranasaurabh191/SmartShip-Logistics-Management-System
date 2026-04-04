using SmartShip.IdentityService.Core.DTOs;

public class LoginResult
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public AuthResponse? Data { get; set; }
}