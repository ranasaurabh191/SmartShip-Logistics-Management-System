using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SmartShip.IdentityService.Data;
using SmartShip.IdentityService.DTOs;
using SmartShip.IdentityService.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SmartShip.IdentityService.Services;

public class AuthService : IAuthService
{
    private readonly IdentityDbContext _context;
    private readonly IConfiguration _config;

    public AuthService(IdentityDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    public async Task<AuthResponse?> SignupAsync(SignupRequest request)
    {
        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            return null;

        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            Phone = request.Phone,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = "CUSTOMER"
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return new AuthResponse(GenerateToken(user), user.Role, user.Name, user.Id);
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return null;

        return new AuthResponse(GenerateToken(user), user.Role, user.Name, user.Id);
    }

    private string GenerateToken(User user)
    {
        var jwt = _config.GetSection("JwtSettings");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(double.Parse(jwt["ExpiryMinutes"]!)),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    public async Task<object> DebugLoginAsync(LoginRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null)
            return new { step = "FAILED", reason = "User not found", email = request.Email };

        var hashMatch = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

        return new
        {
            step = hashMatch ? "SUCCESS" : "FAILED",
            reason = hashMatch ? "Password matches" : "Wrong password",
            email = user.Email,
            role = user.Role,
            isActive = user.IsActive,
            storedHash = user.PasswordHash,
            passwordProvided = request.Password
        };
    }

    public async Task<object> FixAdminAsync()
    {
        var admin = await _context.Users.FirstOrDefaultAsync(u => u.Email == "admin@smartship.com");

        if (admin == null)
        {
            _context.Users.Add(new User
            {
                Name = "Super Admin",
                Email = "admin@smartship.com",
                Phone = "9999999999",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                Role = "ADMIN",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }
        else
        {
            admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123");
        }

        await _context.SaveChangesAsync();
        return new { message = "Admin fixed!", email = "admin@smartship.com", password = "Admin@123" };
    }
}
