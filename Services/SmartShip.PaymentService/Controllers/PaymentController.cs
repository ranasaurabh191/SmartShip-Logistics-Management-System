using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartShip.PaymentService.DTOs;
using SmartShip.PaymentService.Services;

namespace SmartShip.PaymentService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(IPaymentService paymentService, ILogger<PaymentController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    [HttpPost("create-order")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        _logger.LogInformation("Create order request for Shipment {ShipmentId} | Method: {Method}",
                request.ShipmentId, request.PaymentMethod);

      var result = await _paymentService.CreateOrderAsync(request);
        return Ok(result);
    }

    [HttpPost("verify")]
    public async Task<IActionResult> Verify([FromBody] VerifyPaymentRequest request)
    {
        _logger.LogInformation("Verify payment request for Order {OrderId}", request.RazorpayOrderId);
        var result = await _paymentService.VerifyPaymentAsync(request);
        if (result == null) return NotFound(new { message = "Payment record not found." });
        return Ok(result);
    }

    [HttpGet("payment-status")]
    public async Task<IActionResult> PaymentStatus(
    [FromQuery] string? razorpayOrderId,
    [FromQuery] int? shipmentId,
    [FromQuery] string? trackingNumber)
    {
        _logger.LogInformation("Payment status request | OrderId:{OrderId} | ShipmentId:{ShipmentId} | Tracking:{Tracking}",
            razorpayOrderId, shipmentId, trackingNumber);

        var request = new PaymentStatusRequest
        {
            RazorpayOrderId = razorpayOrderId,
            ShipmentId = shipmentId,
            TrackingNumber = trackingNumber
        };

        var result = await _paymentService.PaymentStatusAsync(request);
        if (result == null) return NotFound(new { message = "Payment record not found." });

        return Ok(result);
    }

    [HttpGet("shipment/{shipmentId}")]
    public async Task<IActionResult> GetByShipment(int shipmentId)
    {
        _logger.LogInformation("Fetching payment for Shipment {ShipmentId}", shipmentId);
        var result = await _paymentService.GetByShipmentIdAsync(shipmentId);
        return result != null ? Ok(result) : NotFound("Payment not found");
    }
}