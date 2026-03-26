namespace SmartShip.IdentityService.DTOs;

public record SignupRequest(string Name, string Email, string Phone, string Password);
public record LoginRequest(string Email, string Password);
public record AuthResponse(string Token, string Role, string Name, int UserId);
public record UserDto(int Id, string Name, string Email, string Phone, string Role, bool IsActive, DateTime CreatedAt);
public record UpdateUserRequest(string Name, string Phone, bool IsActive, string Role);
