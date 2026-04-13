namespace SmartShip.IdentityService.Core.DTOs;

public record SignupOtpRequest(string Email, string Name, string Phone, string Password);
public record VerifyOtpRequest(string Email, string Otp, string Name, string Phone, string Password);
public record OtpResponse(
    string Message,
    bool Success,
    string? Token = null,
    string? UserId = null
);