using SmartShip.IdentityService.Core.DTOs;

namespace SmartShip.IdentityService.Core.Interfaces.Services;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<object> DebugLoginAsync(LoginRequest request);  
    Task<object> FixAdminAsync();
    Task<object> RequestSignupOtpAsync(SignupOtpRequest request);  
    Task<OtpResponse> VerifySignupOtpAsync(VerifyOtpRequest request);
    Task<(string token, int userId, string role)> FindOrCreateOAuthUserAsync(string email, string name, string provider);
    Task<object> ForgotPasswordAsync(ForgotPasswordRequest request);
    Task<object> ResetPasswordAsync(ResetPasswordRequest request);
}
