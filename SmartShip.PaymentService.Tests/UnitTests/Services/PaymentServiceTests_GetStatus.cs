using FluentAssertions;
using Moq;
using SmartShip.PaymentService.Core.DTOs;
using SmartShip.PaymentService.Domain.Entities;
using SmartShip.PaymentService.Domain.Entities.Enums;
using SmartShip.PaymentService.Tests.Helpers;
namespace SmartShip.PaymentService.Tests.UnitTests.Services;

public class PaymentServiceTests_GetStatus : PaymentServiceTestBase
{    private ShipmentPayment MakePayment(
        PaymentStatus status = PaymentStatus.Paid,
        PaymentMethod method = PaymentMethod.Online) => new()
        {
            Id = 1,
            ShipmentId = 26,
            TrackingNumber = "SS2026040620173",
            CustomerId = 29,
            Amount = 200,
            PaymentMethod = method,
            PaymentStatus = status,
            RazorpayOrderId = "order_MOCK_123",
            CreatedAt = DateTime.Now,
            PaidAt = status == PaymentStatus.Paid ? DateTime.Now : null
        };

    [Fact]
    public async Task GetByShipmentIdAsync_Found_ReturnsMappedResponse()
    {
        _paymentRepo.Setup(r => r.GetByShipmentIdAsync(26)).ReturnsAsync(MakePayment());
        var result = await BuildService().GetByShipmentIdAsync(26);

        result.ShipmentId.Should().Be(26);
        result.PaymentStatus.Should().Be("Paid");
        result.TrackingNumber.Should().Be("SS2026040620173");
        result.Message.Should().Be("Payment completed successfully.");
    }

    [Fact]
    public async Task GetByShipmentIdAsync_PendingOnline_ReturnsInitiatedMessage()
    {
        _paymentRepo.Setup(r => r.GetByShipmentIdAsync(26))
                    .ReturnsAsync(MakePayment(PaymentStatus.Pending, PaymentMethod.Online));
        var result = await BuildService().GetByShipmentIdAsync(26);
        result.Message.Should().Contain("Please complete payment");
    }

    [Fact]
    public async Task GetByShipmentIdAsync_PendingCOD_ReturnsCODMessage()
    {
        _paymentRepo.Setup(r => r.GetByShipmentIdAsync(26))
                    .ReturnsAsync(MakePayment(PaymentStatus.Pending, PaymentMethod.COD));
        var result = await BuildService().GetByShipmentIdAsync(26);
        result.Message.Should().Contain("COD");
    }

    [Fact]
    public async Task GetByShipmentIdAsync_Failed_ReturnsFailedMessage()
    {
        _paymentRepo.Setup(r => r.GetByShipmentIdAsync(26)).ReturnsAsync(MakePayment(PaymentStatus.Failed));
        var result = await BuildService().GetByShipmentIdAsync(26);
        result.Message.Should().Contain("failed");
    }

    [Fact]
    public async Task GetByShipmentIdAsync_NotFound_ThrowsKeyNotFoundException()
    {
        _paymentRepo.Setup(r => r.GetByShipmentIdAsync(99)).ReturnsAsync((ShipmentPayment?)null);
        var act = async () => await BuildService().GetByShipmentIdAsync(99);
        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("*not found*");
    }

    [Fact]
    public async Task PaymentStatusAsync_ByOrderId_ReturnsCorrectPayment()
    {
        _paymentRepo.Setup(r => r.GetByOrderIdAsync("order_MOCK_123")).ReturnsAsync(MakePayment());
        var result = await BuildService().PaymentStatusAsync(new PaymentStatusRequest { RazorpayOrderId = "order_MOCK_123" });
        result.PaymentStatus.Should().Be("Paid");
    }

    [Fact]
    public async Task PaymentStatusAsync_ByShipmentId_ReturnsCorrectPayment()
    {
        _paymentRepo.Setup(r => r.GetByShipmentIdAsync(26)).ReturnsAsync(MakePayment());
        var result = await BuildService().PaymentStatusAsync(new PaymentStatusRequest { ShipmentId = 26 });
        result.ShipmentId.Should().Be(26);
    }

    [Fact]
    public async Task PaymentStatusAsync_ByTrackingNumber_ReturnsCorrectPayment()
    {
        _paymentRepo.Setup(r => r.GetByTrackingNumberAsync("SS2026040620173")).ReturnsAsync(MakePayment());
        var result = await BuildService().PaymentStatusAsync(new PaymentStatusRequest { TrackingNumber = "SS2026040620173" });
        result.TrackingNumber.Should().Be("SS2026040620173");
    }

    [Fact]
    public async Task PaymentStatusAsync_NotFound_ThrowsKeyNotFoundException()
    {
        _paymentRepo.Setup(r => r.GetByOrderIdAsync(It.IsAny<string>())).ReturnsAsync((ShipmentPayment?)null);
        _paymentRepo.Setup(r => r.GetByShipmentIdAsync(It.IsAny<int>())).ReturnsAsync((ShipmentPayment?)null);
        _paymentRepo.Setup(r => r.GetByTrackingNumberAsync(It.IsAny<string>())).ReturnsAsync((ShipmentPayment?)null);

        var act = async () => await BuildService().PaymentStatusAsync(
            new PaymentStatusRequest { RazorpayOrderId = "order_DOESNOTEXIST" });

        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("*Payment record not found*");
    }
}