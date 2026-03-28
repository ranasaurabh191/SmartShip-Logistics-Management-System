using MassTransit;
using Microsoft.EntityFrameworkCore;
//using Razorpay.Api;
using SmartShip.PaymentService.Data;
using SmartShip.PaymentService.DTOs;
using SmartShip.PaymentService.Models;
using SmartShip.PaymentService.Models.Enums;
using SmartShip.Shared.Events;
namespace SmartShip.PaymentService.Services;

public class PaymentService : IPaymentService
{
    private readonly PaymentDbContext _context;
    private readonly IConfiguration _config;
    private readonly IPublishEndpoint _publisher;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(PaymentDbContext context, IConfiguration config,
        IPublishEndpoint publisher, ILogger<PaymentService> logger)
    {
        _context = context;
        _config = config;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<PaymentResponse?> CreateOrderAsync(CreateOrderRequest request)
    {
        _logger.LogInformation("Creating payment order for Shipment {ShipmentId} | Method: {Method} | Amount: {Amount}",
            request.ShipmentId, request.PaymentMethod, request.Amount);

        var existing = await _context.Payments
        .FirstOrDefaultAsync(p => p.TrackingNumber == request.TrackingNumber);

        if (existing != null)
        {
            if (existing.PaymentStatus == PaymentStatus.Paid)
            {
                _logger.LogWarning("Payment already completed for {TrackingNumber}", request.TrackingNumber);
                return MapToResponse(existing, "You have already paid for this shipment.");
            }

            if (existing.PaymentMethod == PaymentMethod.COD)
            {
                _logger.LogWarning("COD already registered for {TrackingNumber}", request.TrackingNumber);
                return MapToResponse(existing, "COD already registered. Pay on delivery.");
            }

            if (existing.PaymentMethod == PaymentMethod.Online)
            {
                _logger.LogWarning("Online payment already initiated for {TrackingNumber}", request.TrackingNumber);
                return MapToResponse(existing, "Payment already initiated. Please complete your payment.");
            }
        }

        var payment = new ShipmentPayment
        {
            ShipmentId = request.ShipmentId,
            TrackingNumber = request.TrackingNumber,
            CustomerId = request.CustomerId,
            Amount = request.Amount,
            PaymentMethod = request.PaymentMethod,
            PaymentStatus = PaymentStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        if (request.PaymentMethod == PaymentMethod.COD)
        {
            payment.PaymentStatus = PaymentStatus.Pending; 
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            await _publisher.Publish(new PaymentCompletedEvent
            {
                ShipmentId = payment.ShipmentId,
                TrackingNumber = payment.TrackingNumber,
                PaymentMethod = "COD",
                PaymentStatus = "Pending"
            });
            _logger.LogInformation("COD order created for {TrackingNumber}", request.TrackingNumber);

            return MapToResponse(payment);
        }

        //var client = new RazorpayClient(
        //    _config["Razorpay:KeyId"],
        //    _config["Razorpay:KeySecret"]);

        //var options = new Dictionary<string, object>
        //{
        //    { "amount", (int)(request.Amount * 100) },  
        //    { "currency", "INR" },
        //    { "receipt", $"shipment_{request.ShipmentId}" }
        //};

        //var order = client.Order.Create(options);
        //payment.RazorpayOrderId = order["id"].ToString();

        //_context.Payments.Add(payment);
        //await _context.SaveChangesAsync();

        //_logger.LogInformation("Razorpay order created: {OrderId} for {TrackingNumber}",
        //    payment.RazorpayOrderId, request.TrackingNumber);

        //return MapToResponse(payment);

        if (request.PaymentMethod == PaymentMethod.Online)
        {
            payment.RazorpayOrderId = "order_MOCK_" + DateTime.Now.Ticks;

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Mock Razorpay order created: {OrderId} for {TrackingNumber}",
                payment.RazorpayOrderId, payment.TrackingNumber);

            return MapToResponse(payment);
        }

        _logger.LogWarning("Unknown payment method: {Method} for Shipment {ShipmentId}",
           request.PaymentMethod, request.ShipmentId);
        return new PaymentResponse { Message = $"Unsupported payment method: {request.PaymentMethod}" };
    }

    public async Task<PaymentResponse?> VerifyPaymentAsync(VerifyPaymentRequest request)
    {
        _logger.LogInformation("Verifying payment for Order {OrderId}", request.RazorpayOrderId);

        var payment = await _context.Payments.FirstOrDefaultAsync(p => p.RazorpayOrderId == request.RazorpayOrderId);

        if (payment == null)
        {
            _logger.LogWarning("Payment not found for Order {OrderId}", request.RazorpayOrderId);
            return null;
        }

        //var attributes = new Dictionary<string, string>
        //{
        //    { "razorpay_order_id", request.RazorpayOrderId },
        //    { "razorpay_payment_id", request.RazorpayPaymentId },
        //    { "razorpay_signature", request.Signature }
        //};

        //try
        //{
        //    Utils.verifyPaymentSignature(attributes);
        //}
        //catch
        //{
        //    _logger.LogWarning("Signature verification failed for Order {OrderId}", request.RazorpayOrderId);
        //    payment.PaymentStatus = PaymentStatus.Failed;
        //    await _context.SaveChangesAsync();
        //    throw new Exception("Invalid payment signature");
        //}

        payment.PaymentStatus = PaymentStatus.Paid;
        payment.RazorpayPaymentId = request.RazorpayPaymentId;
        payment.RazorpaySignature = request.Signature;
        payment.PaidAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Payment verified -> {TrackingNumber} Paid at {PaidAt}",
            payment.TrackingNumber, payment.PaidAt?.ToLocalTime().ToString("dd-MMM hh:mm tt"));

        await _publisher.Publish(new PaymentCompletedEvent
        {
            ShipmentId = payment.ShipmentId,
            TrackingNumber = payment.TrackingNumber,
            PaymentMethod = "Online",
            PaymentStatus = "Paid"
        });

        return MapToResponse(payment, "Payment successful!");
    }

    public async Task<PaymentResponse?> PaymentStatusAsync(PaymentStatusRequest request)
    {
        _logger.LogInformation("Fetching payment status | OrderId:{OrderId} | ShipmentId:{ShipmentId} | Tracking:{Tracking}",
            request.RazorpayOrderId, request.ShipmentId, request.TrackingNumber);

        ShipmentPayment? payment = null;

        if (!string.IsNullOrEmpty(request.RazorpayOrderId))
        {
            payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.RazorpayOrderId == request.RazorpayOrderId);
        }
        else if (request.ShipmentId.HasValue)
        {
            payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.ShipmentId == request.ShipmentId.Value);
        }
        else if (!string.IsNullOrEmpty(request.TrackingNumber))
        {
            payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.TrackingNumber == request.TrackingNumber);
        }

        if (payment == null)
        {
            _logger.LogWarning("Payment not found for Order {OrderId}", request.RazorpayOrderId);
            return null;
        }

        _logger.LogInformation("Payment status found -> {TrackingNumber} | Status: {Status} | Method: {Method}",
            payment.TrackingNumber, payment.PaymentStatus, payment.PaymentMethod);

        var message = payment.PaymentStatus switch
        {
            PaymentStatus.Paid => "Payment completed successfully.",
            PaymentStatus.Pending when payment.PaymentMethod == PaymentMethod.COD => "COD registered. Pay on delivery.",
            PaymentStatus.Pending => "Payment initiated. Please complete payment.",
            PaymentStatus.Failed => "Payment failed. Please try again.",
            _ => ""
        };

        _logger.LogInformation("Payment status → {TrackingNumber} | {Status} | {Method}",
       payment.TrackingNumber, payment.PaymentStatus, payment.PaymentMethod);

        return new PaymentResponse
        {
            Id = payment.Id,
            ShipmentId = payment.ShipmentId,
            TrackingNumber = payment.TrackingNumber,
            Amount = payment.Amount,
            PaymentMethod = payment.PaymentMethod.ToString(),
            PaymentStatus = payment.PaymentStatus.ToString(),
            RazorpayOrderId = payment.RazorpayOrderId,
            RazorpayPaymentId = payment.RazorpayPaymentId,
            CreatedAt = DateTime.SpecifyKind(payment.CreatedAt, DateTimeKind.Utc)
                .ToLocalTime().ToString("dd-MMM-yyyy hh:mm tt"),  
            PaidAt = payment.PaidAt.HasValue
            ? DateTime.SpecifyKind(payment.PaidAt.Value, DateTimeKind.Utc)
                .ToLocalTime().ToString("dd-MMM-yyyy hh:mm tt")   
            : null,
            Message = message
        };
    }

    public async Task<PaymentResponse?> GetByShipmentIdAsync(int shipmentId)
    {
        _logger.LogInformation("Fetching payment for Shipment {ShipmentId}", shipmentId);

        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.ShipmentId == shipmentId);

        if (payment == null)
        {
            _logger.LogWarning("No payment found for Shipment {ShipmentId}", shipmentId);
            return null;
        }

        return MapToResponse(payment);
    }

    private static PaymentResponse MapToResponse(ShipmentPayment p, string? message = null) => new PaymentResponse
    {
        Id = p.Id,
        ShipmentId = p.ShipmentId,
        TrackingNumber = p.TrackingNumber,
        Amount = p.Amount,
        PaymentMethod = p.PaymentMethod.ToString(),
        PaymentStatus = p.PaymentStatus.ToString(),
        RazorpayOrderId = p.RazorpayOrderId,
        RazorpayPaymentId = p.RazorpayPaymentId,
        CreatedAt = DateTime.SpecifyKind(p.CreatedAt, DateTimeKind.Utc)
                    .ToLocalTime()
                    .ToString("dd-MMM-yyyy hh:mm tt"), 
        PaidAt = p.PaidAt.HasValue
                ? DateTime.SpecifyKind(p.PaidAt.Value, DateTimeKind.Utc)
                    .ToLocalTime()
                    .ToString("dd-MMM-yyyy hh:mm tt")  
                : null,
        Message = message
    };
}