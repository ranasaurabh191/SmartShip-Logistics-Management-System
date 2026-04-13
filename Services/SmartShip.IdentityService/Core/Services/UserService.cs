using MassTransit;
using SmartShip.IdentityService.Core.DTOs;
using SmartShip.IdentityService.Core.Interfaces.Persistence;
using SmartShip.IdentityService.Core.Interfaces.Repositories;
using SmartShip.IdentityService.Core.Interfaces.Services;
using SmartShip.Shared.Events;

namespace SmartShip.IdentityService.Core.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IOtpVerificationRepository _otpRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UserService> _logger;
    private readonly IPublishEndpoint _publisher;
    private readonly IHttpClientFactory _httpClientFactory;
    public UserService(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ILogger<UserService> logger,
        IPublishEndpoint publisher,
        IOtpVerificationRepository otpRepository,
        IHttpClientFactory httpClientFactory)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _publisher = publisher;
        _httpClientFactory = httpClientFactory;
        _otpRepository = otpRepository;
    }

    public async Task<PagedResponse<UserDto>> GetAllUsersPagedAsync(UserPagedRequest req)
    {
        _logger.LogInformation(
            "Fetching paged users | Page: {Page}, PageSize: {PageSize}, Role: {Role}, IsActive: {IsActive}, Search: {Search}",
            req.Page, req.PageSize, req.Role, req.IsActive, req.Search);

        var paged = await _userRepository.GetPagedAsync(req);

        return new PagedResponse<UserDto>
        {
            Data = paged.Data.Select(u => new UserDto(
                u.Id,
                u.Name,
                u.Email,
                u.Phone,
                u.Role,
                u.IsActive,
                u.CreatedAt)),
            TotalCount = paged.TotalCount,
            Page = paged.Page,
            PageSize = paged.PageSize
        };
    }

    public async Task<UserDto> GetUserByIdAsync(int id)
    {
        _logger.LogInformation("Fetching user with ID: {UserId}", id);

        var user = await _userRepository.GetByIdAsync(id);

        if (user == null)
        {
            _logger.LogWarning("User not found with ID: {UserId}", id);
            throw new KeyNotFoundException($"User {id} not found.");
        }

        _logger.LogInformation("User found: {Email}", user.Email);

        return new UserDto(user.Id, user.Name, user.Email, user.Phone, user.Role, user.IsActive, user.CreatedAt);
    }

    public async Task UpdateUserAsync(int id, UpdateUserRequest request)
    {
        _logger.LogInformation("Updating user with ID: {UserId}", id);

        var user = await _userRepository.GetByIdAsync(id);

        if (user == null)
        {
            _logger.LogWarning("Update failed - user not found: {UserId}", id);
            throw new KeyNotFoundException($"User {id} not found.");
        }

        user.Name = request.Name;
        user.Phone = request.Phone;
        user.IsActive = request.IsActive;
        user.Role = request.Role;

        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("User updated successfully: {UserId}", id);
    }

    public async Task DeleteUserAsync(int userId)
    {
        _logger.LogInformation("Deleting user with ID: {UserId}", userId);

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("Delete failed - user not found: {UserId}", userId);
            throw new KeyNotFoundException($"User {userId} not found.");
        }

        var otpEntries = await _otpRepository.GetByUserIdAsync(userId);
        if (otpEntries.Any())
        {
            _logger.LogInformation("Deleting {OtpCount} OTP entries for UserId: {UserId}", otpEntries.Count(), userId);
            foreach (var otp in otpEntries)
            {
                _otpRepository.Delete(otp);
            }
        }


        _userRepository.Delete(user);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("User deleted successfully: {UserId}", userId);

        await _publisher.Publish(new UserDeletedEvent
        {
            UserId = userId,
            Email = user.Email,
            Role = user.Role,
            DeletedAt = DateTime.Now,
        });

        _logger.LogInformation("Delete Event published successfully for User Id : {UserId}", userId);
    }

    public async Task<string> GetUserEmailAsync(int userId)
    {
        _logger.LogInformation("Fetching email for User {UserId}", userId);

        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException($"User {userId} not found.");

        return user.Email;
    }

    public async Task<bool> UserExistsAsync(int userId)
    {
        _logger.LogInformation("Checking user existence for UserId : {UserId}", userId);

        return await _userRepository.ExistsActiveByIdAsync(userId);
    }
}