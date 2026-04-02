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
    private readonly IPublishEndpoint _publisher;
    private readonly ILogger<PaymentService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    public PaymentService(PaymentDbContext context,
        IPublishEndpoint publisher, ILogger<PaymentService> logger, IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _publisher = publisher;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _httpContextAccessor = httpContextAccessor;
    }

    private HttpClient CreateInternalClient(string clientName)
    {
        var httpClient = _httpClientFactory.CreateClient(clientName);
        var token = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
        if (!string.IsNullOrEmpty(token))
            httpClient.DefaultRequestHeaders.Add("Authorization", token);
        return httpClient;
    }

    public async Task<PaymentResponse> CreateOrderAsync(CreateOrderRequest request)
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User
        .FindFirst("userId")?.Value
        ?? _httpContextAccessor.HttpContext?.User
        .FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var authenticatedUserId))
        {
            _logger.LogWarning("JWT userId claim missing or invalid.");
            throw new UnauthorizedAccessException("Unauthorized: Invalid or missing user token.");
        }

        _logger.LogInformation("Create order request for Shipment {ShipmentId} | Method: {Method}",
        request.ShipmentId, request.PaymentMethod);

        var httpClient = CreateInternalClient("ShipmentService");
        var shipmentCheck = await httpClient.GetAsync($"api/shipments/{request.ShipmentId}");

        if (!shipmentCheck.IsSuccessStatusCode)
        {
            _logger.LogWarning("Shipment {ShipmentId} not found. Cannot create payment order.", request.ShipmentId);
            throw new KeyNotFoundException($"Shipment not found for ID {request.ShipmentId}. Please create a shipment first.");
        }

        _logger.LogInformation("Shipment {ShipmentId} verified. Proceeding with payment.", request.ShipmentId);

        var shipment = await shipmentCheck.Content.ReadFromJsonAsync<ShipmentDTOs>();

        if (shipment == null) throw new KeyNotFoundException("Failed to read shipment details.");

        if (shipment.CustomerId != authenticatedUserId)
        {
            _logger.LogWarning("Ownership mismatch: Token userId={AuthUserId} but Shipment {ShipmentId} belongs to CustomerId={ShipmentOwner}",
                authenticatedUserId, request.ShipmentId, shipment.CustomerId);
            throw new UnauthorizedAccessException("You are not authorized to pay for this shipment.");
        }

        _logger.LogInformation("Shipment {ShipmentId} verified | TrackingNumber: {TrackingNumber}",
            request.ShipmentId, shipment.TrackingNumber);

        var existing = await _context.Payments.FirstOrDefaultAsync(p => p.ShipmentId == request.ShipmentId);

        if (existing != null)
        {
            if (existing.PaymentStatus == PaymentStatus.Paid)
                throw new InvalidOperationException("You have already paid for this shipment.");
            if (existing.PaymentMethod == PaymentMethod.COD)
                throw new InvalidOperationException("COD already registered. Pay on delivery.");
            if (existing.PaymentMethod == PaymentMethod.Online)
                throw new InvalidOperationException("Payment already initiated. Please complete your payment.");
        }

        var payment = new ShipmentPayment
        {
            ShipmentId = request.ShipmentId,
            TrackingNumber = shipment.TrackingNumber,
            CustomerId = authenticatedUserId,
            Amount = shipment.ShippingRate,
            PaymentMethod = request.PaymentMethod,
            PaymentStatus = PaymentStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        var correlation = await _context.SagaCorrelations.FirstOrDefaultAsync(x => x.ShipmentId == request.ShipmentId);
        if (correlation == null || correlation.CorrelationId == Guid.Empty)
        {
            _logger.LogWarning("No valid CorrelationId found for Shipment {ShipmentId}. Saga will not be updated.", request.ShipmentId);
        }
        payment.SagaCorrelationId = correlation?.CorrelationId ?? Guid.Empty;

        if (request.PaymentMethod == PaymentMethod.COD)
        {
            payment.PaymentStatus = PaymentStatus.Pending; 
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            await _publisher.Publish(new PaymentCompletedEvent
            {
                CorrelationId = correlation?.CorrelationId ?? Guid.Empty,
                ShipmentId = payment.ShipmentId,
                TrackingNumber = payment.TrackingNumber,
                PaymentMethod = "COD",
                PaymentStatus = "Pending",
                CustomerId = payment.CustomerId
            });
            _logger.LogInformation("Event published for COD with {ShipmentId}", request.ShipmentId);
            _logger.LogInformation("COD order created for {ShipmentId}", request.ShipmentId);

            return MapToResponse(payment, "COD order created. Pay on delivery.");
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

            _logger.LogInformation("Mock Razorpay order created: {OrderId} for  {ShipmentId}",
                payment.RazorpayOrderId, payment.ShipmentId);

            return MapToResponse(payment, "Online payment order created. Please complete payment.");
        }

        _logger.LogWarning("Unknown payment method: {Method} for Shipment {ShipmentId}", request.PaymentMethod, request.ShipmentId);
        throw new ArgumentException($"Unsupported payment method: {request.PaymentMethod}");
    }

    public async Task<PaymentResponse> VerifyPaymentAsync(VerifyPaymentRequest request)
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User
        .FindFirst("userId")?.Value
        ?? _httpContextAccessor.HttpContext?.User
        .FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var authenticatedUserId))
        {
            _logger.LogWarning("JWT userId claim missing during payment verification.");
            throw new UnauthorizedAccessException("Unauthorized: Invalid or missing user token.");
        }

        _logger.LogInformation("Verifying payment for Order {OrderId}", request.RazorpayOrderId);

        var payment = await _context.Payments.FirstOrDefaultAsync(p =>
        p.RazorpayOrderId == request.RazorpayOrderId &&
        p.ShipmentId == request.ShipmentId);

        if (payment == null)
        {
            _logger.LogWarning("Invalid OrderId {OrderId} for ShipmentId {ShipmentId} — publishing PaymentFailedEvent.",
                request.RazorpayOrderId, request.ShipmentId);

            var existingPayment = await _context.Payments.FirstOrDefaultAsync(p => p.ShipmentId == (request.ShipmentId ?? 0));

            if (existingPayment != null)
            {
                existingPayment.PaymentStatus = PaymentStatus.Failed;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Payment marked as Failed for ShipmentId {ShipmentId} due to failure.", request.ShipmentId);
            }


            var failCorrelation = await _context.SagaCorrelations
                .FirstOrDefaultAsync(x => x.ShipmentId == request.ShipmentId);

            if (failCorrelation != null && failCorrelation.CorrelationId != Guid.Empty)
            {
                await _publisher.Publish(new PaymentFailedEvent
                {
                    CorrelationId = failCorrelation.CorrelationId,
                    ShipmentId = request.ShipmentId ?? 0,
                    TrackingNumber = "",  
                    CustomerId = authenticatedUserId,
                    Reason = $"Invalid Order ID: {request.RazorpayOrderId}"
                });

                _logger.LogInformation("PaymentFailedEvent published for ShipmentId {ShipmentId}", request.ShipmentId);
            }

            throw new KeyNotFoundException($"Invalid Order ID '{request.RazorpayOrderId}'. Payment failed.");
        }


        if (payment.CustomerId != authenticatedUserId)
        {
            _logger.LogWarning("Ownership mismatch: Token userId={AuthUserId} but Payment belongs to CustomerId={Owner}",
                authenticatedUserId, payment.CustomerId);
            throw new UnauthorizedAccessException("You are not authorized to verify this payment.");
        }

        if (payment.PaymentStatus == PaymentStatus.Paid)
            throw new InvalidOperationException("Payment already verified and completed.");

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
        //if (request.Signature == "fail")
        //{
        //    _logger.LogWarning("Signature verification failed for Order {OrderId}", request.RazorpayOrderId);

        //    payment.PaymentStatus = PaymentStatus.Failed;
        //    await _context.SaveChangesAsync();

        //    var correlation = await _context.SagaCorrelations
        //        .FirstOrDefaultAsync(x => x.ShipmentId == payment.ShipmentId);

        //    if (correlation != null && correlation.CorrelationId != Guid.Empty)
        //    {
        //        await _publisher.Publish(new PaymentFailedEvent
        //        {
        //            CorrelationId = correlation.CorrelationId,
        //            ShipmentId = payment.ShipmentId,
        //            TrackingNumber = payment.TrackingNumber,
        //            CustomerId = payment.CustomerId,
        //            Reason = "Payment signature verification failed."
        //        });

        //        _logger.LogInformation("PaymentFailedEvent published for failed signature | ShipmentId {ShipmentId}",
        //            payment.ShipmentId);
        //    }

        //    throw new InvalidOperationException("Invalid payment signature. Payment failed.");
        //}
        payment.PaymentStatus = PaymentStatus.Paid;
        payment.RazorpayPaymentId = request.RazorpayPaymentId;
        payment.RazorpaySignature = request.Signature;
        payment.PaidAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Payment verified -> {TrackingNumber} Paid at {PaidAt}",
            payment.TrackingNumber, payment.PaidAt?.ToLocalTime().ToString("dd-MMM hh:mm tt"));

        var correlation = await _context.SagaCorrelations
        .FirstOrDefaultAsync(x => x.ShipmentId == payment.ShipmentId);

        _logger.LogInformation("Saga CorrelationId for Shipment {ShipmentId}: {CorrelationId}",
            payment.ShipmentId, correlation?.CorrelationId);


        await _publisher.Publish(new PaymentCompletedEvent
        {
            CorrelationId = correlation?.CorrelationId ?? Guid.Empty,
            ShipmentId = payment.ShipmentId,
            TrackingNumber = payment.TrackingNumber,
            PaymentMethod = "Online",
            PaymentStatus = "Paid",
            CustomerId = payment.CustomerId
        });
        _logger.LogInformation("Event published for Online Payment with ShipmentID: {ShipmentId}", request.ShipmentId);

        return MapToResponse(payment, "Payment successful!");
    }

    public async Task<PaymentResponse> PaymentStatusAsync(PaymentStatusRequest request)
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
            throw new KeyNotFoundException("Payment record not found.");
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

    public async Task<PaymentResponse> GetByShipmentIdAsync(int shipmentId)
    {
        _logger.LogInformation("Fetching payment for Shipment {ShipmentId}", shipmentId);

        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.ShipmentId == shipmentId);

        if (payment == null)
        {
            _logger.LogWarning("Payment not found for Shipment {ShipmentId}", shipmentId);
            throw new KeyNotFoundException($"Payment record not found for Shipment {shipmentId}.");
        }

        var message = payment.PaymentStatus switch
        {
            PaymentStatus.Paid => "Payment completed successfully.",
            PaymentStatus.Pending when payment.PaymentMethod == PaymentMethod.COD => "COD registered. Pay on delivery.",
            PaymentStatus.Pending => "Payment initiated. Please complete payment.",
            PaymentStatus.Failed => "Payment failed. Please try again.",
            _ => null
        };

        return MapToResponse(payment, message);
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