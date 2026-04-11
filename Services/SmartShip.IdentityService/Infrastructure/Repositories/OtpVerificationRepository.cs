using Microsoft.EntityFrameworkCore;
using SmartShip.IdentityService.Core.Interfaces.Repositories;
using SmartShip.IdentityService.Domain.Entities;
using SmartShip.IdentityService.Infrastructure.Data;

namespace SmartShip.IdentityService.Infrastructure.Repositories;

public class OtpVerificationRepository : IOtpVerificationRepository
{
    private readonly IdentityDbContext _context;

    public OtpVerificationRepository(IdentityDbContext context) => _context = context;
    
    public async Task<OtpVerification?> GetByEmailAndPurposeAsync(string email, string purpose)
        => await _context.OtpVerifications.FirstOrDefaultAsync(o => o.Email == email && o.Purpose == purpose);

    public async Task AddAsync(OtpVerification otp) => await _context.OtpVerifications.AddAsync(otp);

    public void Update(OtpVerification otp) => _context.OtpVerifications.Update(otp);

    public void Delete(OtpVerification otp) => _context.OtpVerifications.Remove(otp);
}