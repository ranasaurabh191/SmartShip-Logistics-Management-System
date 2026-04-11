using SmartShip.IdentityService.Core.DTOs;

namespace SmartShip.IdentityService.Core.Interfaces.Services;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<object> DebugLoginAsync(LoginRequest request);  
    Task<object> FixAdminAsync();
    Task<object> RequestSignupOtpAsync(SignupOtpRequest request);  
    Task<OtpResponse> VerifySignupOtpAsync(VerifyOtpRequest request);
}
