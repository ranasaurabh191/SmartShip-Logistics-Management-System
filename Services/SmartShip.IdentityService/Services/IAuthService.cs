using SmartShip.IdentityService.DTOs;

namespace SmartShip.IdentityService.Services;

public interface IAuthService
{
    Task<AuthResponse?> SignupAsync(SignupRequest request);
    Task<AuthResponse?> LoginAsync(LoginRequest request);
    Task<object> DebugLoginAsync(LoginRequest request);  
    Task<object> FixAdminAsync();
}
