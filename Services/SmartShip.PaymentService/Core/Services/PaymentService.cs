using MassTransit;
using SmartShip.PaymentService.Core.DTOs;
using SmartShip.PaymentService.Core.Interfaces.Persistence;
using SmartShip.PaymentService.Core.Interfaces.Repositories;
using SmartShip.PaymentService.Core.Interfaces.Services;
using SmartShip.PaymentService.Domain.Entities;
using SmartShip.PaymentService.Domain.Entities.Enums;
using SmartShip.Shared.Events;

namespace SmartShip.PaymentService.Core.Services;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly ISagaCorrelationRepository _sagaCorrelationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublishEndpoint _publisher;
    private readonly ILogger<PaymentService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IRazorpayClient _razorpayClient;

    public PaymentService(
        IPaymentRepository paymentRepository,
        ISagaCorrelationRepository sagaCorrelationRepository,
        IUnitOfWork unitOfWork,
        IPublishEndpoint publisher,
        ILogger<PaymentService> logger,
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor httpContextAccessor,
        IRazorpayClient razorpayClient)
    {
        _paymentRepository = paymentRepository;
        _sagaCorrelationRepository = sagaCorrelationRepository;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _httpContextAccessor = httpContextAccessor;
        _razorpayClient = razorpayClient;
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

        var shipment = await shipmentCheck.Content.ReadFromJsonAsync<ShipmentDTOs>();
        if (shipment == null)
            throw new KeyNotFoundException("Failed to read shipment details.");

        if (shipment.CustomerId != authenticatedUserId)
        {
            _logger.LogWarning("Ownership mismatch: Token userId={AuthUserId} but Shipment {ShipmentId} belongs to CustomerId={ShipmentOwner}",
                authenticatedUserId, request.ShipmentId, shipment.CustomerId);
            throw new UnauthorizedAccessException("You are not authorized to pay for this shipment.");
        }

        _logger.LogInformation("Shipment {ShipmentId} verified | TrackingNumber: {TrackingNumber}",
            request.ShipmentId, shipment.TrackingNumber);

        var existing = await _paymentRepository.GetByShipmentIdAsync(request.ShipmentId);

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
            CreatedAt = DateTime.Now
        };

        var correlation = await _sagaCorrelationRepository.GetByShipmentIdAsync(request.ShipmentId);
        if (correlation == null || correlation.CorrelationId == Guid.Empty)
        {
            _logger.LogWarning("No valid CorrelationId found for Shipment {ShipmentId}. Saga will not be updated.", request.ShipmentId);
        }

        payment.SagaCorrelationId = correlation?.CorrelationId ?? Guid.Empty;

        if (request.PaymentMethod == PaymentMethod.COD)
        {
            payment.PaymentStatus = PaymentStatus.Pending;

            await _paymentRepository.AddAsync(payment);
            await _unitOfWork.SaveChangesAsync();

            await _publisher.Publish(new PaymentCreatedEvent
            {
                ShipmentId = payment.ShipmentId,
                TrackingNumber = payment.TrackingNumber,
                CustomerId = payment.CustomerId,
                PaymentMethod = payment.PaymentMethod.ToString(),
                Amount = payment.Amount
            });

            _logger.LogInformation("PaymentCreatedEvent published for COD with {ShipmentId}", request.ShipmentId);

            await _publisher.Publish(new PaymentCompletedEvent
            {
                CorrelationId = correlation?.CorrelationId ?? Guid.Empty,
                ShipmentId = payment.ShipmentId,
                TrackingNumber = payment.TrackingNumber,
                PaymentMethod = "COD",
                PaymentStatus = "Pending",
                CustomerId = payment.CustomerId,
                Amount = payment.Amount,
                PaidAt = DateTime.Now.ToString("dd-MMM-yyyy hh:mm tt"),
                RazorpayPaymentId = null,
                RazorpayOrderId = null
            });

            _logger.LogInformation("PaymentCompletedEvent published for COD with {ShipmentId}", request.ShipmentId);

            _logger.LogInformation("COD order created for {ShipmentId}", request.ShipmentId);

            return MapToResponse(payment, "COD order created. Pay on delivery.");
        }

        if (request.PaymentMethod == PaymentMethod.Online)
        {
            try
            {
                var razorpayOrderId = _razorpayClient.CreateOrder(shipment.ShippingRate, request.ShipmentId);
                payment.RazorpayOrderId = razorpayOrderId;
                _logger.LogInformation("Razorpay order created: {OrderId}", razorpayOrderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create Razorpay order for Shipment {ShipmentId}", request.ShipmentId);
                throw new InvalidOperationException("Failed to initiate payment. Please try again.");
            }


            await _paymentRepository.AddAsync(payment);
            await _unitOfWork.SaveChangesAsync();

            await _publisher.Publish(new PaymentCreatedEvent
            {
                ShipmentId = payment.ShipmentId,
                TrackingNumber = payment.TrackingNumber,
                CustomerId = payment.CustomerId,
                PaymentMethod = payment.PaymentMethod.ToString(),
                Amount = payment.Amount
            });

            _logger.LogInformation("PaymentCreatedEvent published for Online Payment with {ShipmentId}", request.ShipmentId);

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

        var payment = await _paymentRepository.GetByOrderIdAsync(request.RazorpayOrderId);

        if (payment == null)
        {
            _logger.LogWarning("Order {OrderId} not found — publishing PaymentFailedEvent.", request.RazorpayOrderId);

            ShipmentPayment? existingPayment = null;
            if (request.ShipmentId.HasValue)
            {
                existingPayment = await _paymentRepository.GetByShipmentIdAsync(request.ShipmentId.Value);
                if (existingPayment != null)
                {
                    existingPayment.PaymentStatus = PaymentStatus.Failed;
                    _paymentRepository.Update(existingPayment);
                    await _unitOfWork.SaveChangesAsync();
                }
            }

            var failCorrelation = request.ShipmentId.HasValue
                ? await _sagaCorrelationRepository.GetByShipmentIdAsync(request.ShipmentId.Value)
                : null;

            await _publisher.Publish(new PaymentFailedEvent
            {
                CorrelationId = failCorrelation?.CorrelationId ?? Guid.Empty,
                ShipmentId = request.ShipmentId ?? 0,
                TrackingNumber = existingPayment?.TrackingNumber ?? string.Empty,
                CustomerId = authenticatedUserId,
                Reason = $"Invalid Order ID: {request.RazorpayOrderId}",
                FailedAt = DateTime.Now
            });

            throw new KeyNotFoundException($"Invalid Order ID '{request.RazorpayOrderId}'. Payment failed.");
        }

        if (payment.CustomerId != authenticatedUserId)
        {
            _logger.LogWarning(
                "Unauthorized verify attempt: Token userId={AuthUserId} tried to verify Order {OrderId} belonging to CustomerId={Owner}",
                authenticatedUserId, request.RazorpayOrderId, payment.CustomerId);
            throw new UnauthorizedAccessException("You are not authorized to verify this payment.");
        }

        if (payment.PaymentStatus == PaymentStatus.Paid)
            throw new InvalidOperationException("Payment already verified and completed.");

        var isSignatureValid = _razorpayClient.VerifySignature(request.RazorpayOrderId, request.RazorpayPaymentId, request.Signature);

        if (!isSignatureValid)
        {
            _logger.LogWarning("Razorpay signature mismatch for Order {OrderId}", request.RazorpayOrderId);

            payment.PaymentStatus = PaymentStatus.Failed;
            _paymentRepository.Update(payment);
            await _unitOfWork.SaveChangesAsync();

            var failCorrelation = await _sagaCorrelationRepository.GetByShipmentIdAsync(payment.ShipmentId);
            await _publisher.Publish(new PaymentFailedEvent
            {
                CorrelationId = failCorrelation?.CorrelationId ?? Guid.Empty,
                ShipmentId = payment.ShipmentId,
                TrackingNumber = payment.TrackingNumber,
                CustomerId = authenticatedUserId,
                Reason = "Payment signature verification failed.",
                FailedAt = DateTime.Now
            });

            throw new InvalidOperationException("Payment signature verification failed. This payment has been flagged.");
        }

        payment.PaymentStatus = PaymentStatus.Paid;
        payment.RazorpayPaymentId = request.RazorpayPaymentId;
        payment.RazorpaySignature = request.Signature;
        payment.PaidAt = DateTime.Now;

        _paymentRepository.Update(payment);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Payment verified -> {TrackingNumber} Paid at {PaidAt}",
            payment.TrackingNumber, payment.PaidAt?.ToLocalTime().ToString("dd-MMM hh:mm tt"));

        var correlation = await _sagaCorrelationRepository.GetByShipmentIdAsync(payment.ShipmentId);

        await _publisher.Publish(new PaymentCompletedEvent
        {
            CorrelationId = correlation?.CorrelationId ?? Guid.Empty,
            ShipmentId = payment.ShipmentId,
            TrackingNumber = payment.TrackingNumber,
            PaymentMethod = "Online",
            PaymentStatus = "Paid",
            CustomerId = payment.CustomerId,
            Amount = payment.Amount,
            PaidAt = payment.PaidAt?.ToLocalTime().ToString("dd-MMM-yyyy hh:mm tt"),
            RazorpayPaymentId = payment.RazorpayPaymentId,
            RazorpayOrderId = payment.RazorpayOrderId
        });

        _logger.LogInformation("PaymentCompletedEvent published for ShipmentID: {ShipmentId}", payment.ShipmentId);

        return MapToResponse(payment, "Payment successful!");
    }

    public async Task<PaymentResponse> PaymentStatusAsync(PaymentStatusRequest request)
    {
        _logger.LogInformation("Fetching payment status | OrderId:{OrderId} | ShipmentId:{ShipmentId} | Tracking:{Tracking}",
            request.RazorpayOrderId, request.ShipmentId, request.TrackingNumber);

        ShipmentPayment? payment = null;

        if (!string.IsNullOrEmpty(request.RazorpayOrderId))
            payment = await _paymentRepository.GetByOrderIdAsync(request.RazorpayOrderId);
        else if (request.ShipmentId.HasValue)
            payment = await _paymentRepository.GetByShipmentIdAsync(request.ShipmentId.Value);
        else if (!string.IsNullOrEmpty(request.TrackingNumber))
            payment = await _paymentRepository.GetByTrackingNumberAsync(request.TrackingNumber);

        if (payment == null)
        {
            _logger.LogWarning("Payment not found for Order {OrderId}", request.RazorpayOrderId);
            throw new KeyNotFoundException("Payment record not found.");
        }

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
            CreatedAt = payment.CreatedAt.ToString("dd-MMM-yyyy hh:mm tt"),
            PaidAt = payment.PaidAt?.ToString("dd-MMM-yyyy hh:mm tt"),
            Message = message
        };
    }
    public async Task<List<PaymentResponse>> GetMyPaymentsAsync()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User
            .FindFirst("userId")?.Value
            ?? _httpContextAccessor.HttpContext?.User
            .FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var customerId))
            throw new UnauthorizedAccessException("Unauthorized.");
        
        var payments = await _paymentRepository.GetByCustomerIdAsync(customerId);

        return payments
            .OrderBy(x => x.CreatedAt)
            .Select(x => MapToResponse(x))
            .ToList();
    }

    public async Task<List<PaymentResponse>> GetAllPaymentsAsync()
    {
        var payments = await _paymentRepository.GetAllAsync();

        return payments
            .OrderBy(x => x.CreatedAt)
            .Select(x => MapToResponse(x))
            .ToList();
    }
    public async Task<PaymentResponse> GetByShipmentIdAsync(int shipmentId)
    {
        _logger.LogInformation("Fetching payment for Shipment {ShipmentId}", shipmentId);

        var payment = await _paymentRepository.GetByShipmentIdAsync(shipmentId);

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
        CreatedAt = p.CreatedAt.ToString("dd-MMM-yyyy hh:mm tt"),
        PaidAt = p.PaidAt?.ToString("dd-MMM-yyyy hh:mm tt"),
        Message = message
    };
}