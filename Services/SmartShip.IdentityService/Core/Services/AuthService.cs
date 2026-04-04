using MassTransit;
using Microsoft.IdentityModel.Tokens;
using SmartShip.IdentityService.Core.DTOs;
using SmartShip.IdentityService.Core.Interfaces.Persistence;
using SmartShip.IdentityService.Core.Interfaces.Repositories;
using SmartShip.IdentityService.Core.Interfaces.Services;
using SmartShip.IdentityService.Domain.Entities;
using SmartShip.NotificationService.Core.Interfaces.Services;
using SmartShip.Shared.Events;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SmartShip.IdentityService.Core.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IOtpVerificationRepository _otpRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthService> _logger;
    private readonly IPublishEndpoint _publisher;
    private readonly IEmailService _emailService;

    public AuthService(
        IUserRepository userRepository,
        IOtpVerificationRepository otpRepository,
        IUnitOfWork unitOfWork,
        IConfiguration config,
        ILogger<AuthService> logger,
        IPublishEndpoint publisher,
        IEmailService emailService)
    {
        _userRepository = userRepository;
        _otpRepository = otpRepository;
        _unitOfWork = unitOfWork;
        _config = config;
        _logger = logger;
        _publisher = publisher;
        _emailService = emailService;
    }

    public async Task<AuthResponse?> SignupAsync(SignupRequest request)
    {
        _logger.LogInformation("Signup attempt for email: {Email}", request.Email);

        if (await _userRepository.ExistsByEmailAsync(request.Email))
        {
            _logger.LogWarning("Signup failed - email already exists: {Email}", request.Email);
            throw new InvalidOperationException("Email already exists.");
        }

        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            Phone = request.Phone,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = "CUSTOMER"
        };

        await _userRepository.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("User created successfully: {Email} | Role: {Role}", user.Email, user.Role);

        await _publisher.Publish(new UserCreatedEvent
        {
            UserId = user.Id,
            Email = user.Email,
            Name = user.Name,
            Role = user.Role,
            CreatedAt = user.CreatedAt
        });

        _logger.LogInformation("User creation event published.");

        return new AuthResponse(GenerateToken(user), user.Role, user.Name, user.Id);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        _logger.LogInformation("Login attempt: {Email}", request.Email);

        var user = await _userRepository.GetByEmailAsync(request.Email);

        if (user == null)
        {
            _logger.LogWarning("Login failed - user not found: {Email}", request.Email);
            throw new KeyNotFoundException("User not found with this email. Please signup.");
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Login failed - inactive user: {Email}", request.Email);
            throw new UnauthorizedAccessException("User account is inactive.");
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Login failed - wrong password: {Email}", request.Email);
            throw new UnauthorizedAccessException("Incorrect password.");
        }

        _logger.LogInformation("Login successful: {Email} | Role: {Role}", user.Email, user.Role);
        return new AuthResponse(GenerateToken(user), user.Role, user.Name, user.Id);
    }

    private string GenerateToken(User user)
    {
        _logger.LogInformation("Generating JWT token for user: {Email}, Role: {Role}", user.Email, user.Role);

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

        var expiryMinutes = double.Parse(jwt["ExpiryMinutes"]!);

        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,
            expires: DateTime.Now.AddMinutes(expiryMinutes),
            signingCredentials: creds
        );

        _logger.LogInformation("Token generated successfully for user: {Email}, expires in {Minutes} minutes", user.Email, expiryMinutes);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateOtp() => Random.Shared.Next(100000, 999999).ToString();
    private static string HashOtp(string otp) => BCrypt.Net.BCrypt.HashPassword(otp);
    private static bool VerifyOtp(string enteredOtp, string storedHash) => BCrypt.Net.BCrypt.Verify(enteredOtp, storedHash);

    public async Task<OtpResponse> RequestSignupOtpAsync(SignupOtpRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            throw new ArgumentException("Email is required");

        _logger.LogInformation("OTP request for signup | Email: {Email}", request.Email);

        if (await _userRepository.ExistsByEmailAsync(request.Email))
            throw new InvalidOperationException("Email already registered. Please login.");

        var existingOtp = await _otpRepository.GetByEmailAndPurposeAsync(request.Email, "Signup");

        var otp = GenerateOtp();
        var otpHash = HashOtp(otp);
        var expiresAt = DateTime.UtcNow.AddMinutes(5);

        if (existingOtp != null)
        {
            existingOtp.OtpHash = otpHash;
            existingOtp.ExpiresAt = expiresAt;
            existingOtp.IsUsed = false;
            _otpRepository.Update(existingOtp);
        }
        else
        {
            existingOtp = new OtpVerification
            {
                Email = request.Email,
                Purpose = "Signup",
                OtpHash = otpHash,
                ExpiresAt = expiresAt
            };

            await _otpRepository.AddAsync(existingOtp);
        }

        await _unitOfWork.SaveChangesAsync();

        await _emailService.SendOtpEmailAsync(request.Email, otp);

        return new OtpResponse("Verify the OTP sent to your email. It expires in 5 minutes.", true);
    }

    public async Task<OtpResponse> VerifySignupOtpAsync(VerifyOtpRequest request)
    {
        _logger.LogInformation("OTP verification for signup | Email: {Email}", request.Email);

        var otpRecord = await _otpRepository.GetByEmailAndPurposeAsync(request.Email, "Signup");

        if (otpRecord == null)
            throw new KeyNotFoundException("No OTP found for this email. Please request a new one.");

        if (otpRecord.IsUsed)
            throw new InvalidOperationException("OTP already used. Please request a new one.");

        if (DateTime.UtcNow > otpRecord.ExpiresAt)
        {
            _otpRepository.Delete(otpRecord);
            await _unitOfWork.SaveChangesAsync();
            throw new InvalidOperationException("OTP expired. Please request a new one.");
        }

        if (!VerifyOtp(request.Otp, otpRecord.OtpHash))
            throw new InvalidOperationException("Invalid OTP. Please try again.");

        otpRecord.IsUsed = true;
        _otpRepository.Update(otpRecord);
        await _unitOfWork.SaveChangesAsync();

        var user = new User
        {
            Name = request.Name ?? "User",
            Email = request.Email,
            Phone = request.Phone ?? "",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = "CUSTOMER",
            IsActive = true
        };

        await _userRepository.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("User created via OTP | Email: {Email}", request.Email);

        await _publisher.Publish(new UserCreatedEvent
        {
            UserId = user.Id,
            Email = user.Email,
            Name = user.Name,
            Role = user.Role,
            CreatedAt = user.CreatedAt
        });

        _logger.LogInformation("UserCreatedEvent Published.");

        var token = GenerateToken(user);
        return new OtpResponse("Account created successfully!", true, token, user.Id.ToString());
    }

    public async Task<object> DebugLoginAsync(LoginRequest request)
    {
        _logger.LogInformation("Debug login attempt for email: {Email}", request.Email);

        var user = await _userRepository.GetByEmailAsync(request.Email);

        if (user == null)
        {
            _logger.LogWarning("Debug login failed - user not found: {Email}", request.Email);
            return new { step = "FAILED", reason = "User not found", email = request.Email };
        }

        var hashMatch = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

        if (hashMatch)
            _logger.LogInformation("Debug login SUCCESS for: {Email}", user.Email);
        else
            _logger.LogWarning("Debug login FAILED - wrong password: {Email}", user.Email);

        return new
        {
            step = hashMatch ? "SUCCESS" : "FAILED",
            reason = hashMatch ? "Password matches" : "Wrong password",
            email = user.Email,
            role = user.Role,
            isActive = user.IsActive
        };
    }

    public async Task<object> FixAdminAsync()
    {
        _logger.LogInformation("FixAdmin operation started");

        var admin = await _userRepository.GetByEmailForAdminAsync("admin@smartship.com");

        if (admin == null)
        {
            _logger.LogWarning("Admin not found. Creating new admin user.");

            admin = new User
            {
                Name = "Super Admin",
                Email = "admin@smartship.com",
                Phone = "9999999999",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                Role = "ADMIN",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _userRepository.AddAsync(admin);
        }
        else
        {
            _logger.LogWarning("Admin already exists. Resetting password.");

            admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123");
            _userRepository.Update(admin);
        }

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("FixAdmin operation completed successfully.");

        return new
        {
            message = "Admin fixed!",
            email = "admin@smartship.com",
            password = "Admin@123"
        };
    }
}