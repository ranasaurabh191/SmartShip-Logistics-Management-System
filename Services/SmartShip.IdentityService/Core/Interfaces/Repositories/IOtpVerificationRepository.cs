using SmartShip.IdentityService.Domain.Entities;

namespace SmartShip.IdentityService.Core.Interfaces.Repositories;

public interface IOtpVerificationRepository
{
    Task<OtpVerification?> GetByEmailAndPurposeAsync(string email, string purpose);
    Task AddAsync(OtpVerification otpVerification);
    void Update(OtpVerification otpVerification);
    void Delete(OtpVerification otpVerification);
}