using Microsoft.EntityFrameworkCore;
using SmartShip.PaymentService.Core.Interfaces.Repositories;
using SmartShip.PaymentService.Domain.Entities;
using SmartShip.PaymentService.Infrastructure.Data;

namespace SmartShip.PaymentService.Infrastructure.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly PaymentDbContext _context;

    public PaymentRepository(PaymentDbContext context)
    {
        _context = context;
    }

    public async Task<ShipmentPayment?> GetByShipmentIdAsync(int shipmentId)
    {
        return await _context.Payments
            .FirstOrDefaultAsync(p => p.ShipmentId == shipmentId);
    }

    public async Task<ShipmentPayment?> GetByOrderAndShipmentAsync(string? razorpayOrderId, int? shipmentId)
    {
        return await _context.Payments
            .FirstOrDefaultAsync(p =>
                p.RazorpayOrderId == razorpayOrderId &&
                p.ShipmentId == shipmentId);
    }

    public async Task<ShipmentPayment?> GetByOrderIdAsync(string razorpayOrderId)
    {
        return await _context.Payments
            .FirstOrDefaultAsync(p => p.RazorpayOrderId == razorpayOrderId);
    }

    public async Task<ShipmentPayment?> GetByTrackingNumberAsync(string trackingNumber)
    {
        return await _context.Payments
            .FirstOrDefaultAsync(p => p.TrackingNumber == trackingNumber);
    }

    public async Task AddAsync(ShipmentPayment payment)
    {
        await _context.Payments.AddAsync(payment);
    }

    public void Update(ShipmentPayment payment)
    {
        _context.Payments.Update(payment);
    }
}