using SmartShip.IdentityService.Core.DTOs;

namespace SmartShip.IdentityService.Core.Interfaces.Services;

public interface IAuthService
{
    Task<AuthResponse?> SignupAsync(SignupRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<object> DebugLoginAsync(LoginRequest request);  
    Task<object> FixAdminAsync();
    Task<OtpResponse> RequestSignupOtpAsync(SignupOtpRequest request);  
    Task<OtpResponse> VerifySignupOtpAsync(VerifyOtpRequest request);
}
