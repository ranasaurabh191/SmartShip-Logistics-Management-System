using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SmartShip.PaymentService.Core.DTOs;
using SmartShip.PaymentService.Core.Interfaces.Persistence;
using SmartShip.PaymentService.Core.Interfaces.Repositories;
using SmartShip.PaymentService.Domain.Entities;
using SmartShip.PaymentService.Domain.Entities.Enums;
using SmartShip.PaymentService.Tests.Helpers;
using SmartShip.PaymentService.Tests.Mocks;
using SmartShip.Shared.Events;
using PaymentSvc = SmartShip.PaymentService.Core.Services.PaymentService;

namespace SmartShip.PaymentService.Tests.UnitTests.Services;

public class PaymentServiceTests_VerifyPayment
{
    private readonly Mock<IPaymentRepository> _paymentRepo = new();
    private readonly Mock<ISagaCorrelationRepository> _sagaRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly MockPublishEndpoint _publisher = new();

    private const int UserId = 29;
    private const int ShipmentId = 26;
    private const string OrderId = "order_MOCK_123456";
    private readonly Guid _correlationId = Guid.NewGuid();

    private PaymentSvc BuildService() =>
        new(_paymentRepo.Object, _sagaRepo.Object, _uow.Object, _publisher,
            NullLogger<PaymentSvc>.Instance,
            MockHttpClientFactory.WithNotFound(),
            MockHttpContext.WithUserId(UserId));

    private ShipmentPayment DefaultPayment() => new()
    {
        Id = 1,
        ShipmentId = ShipmentId,
        TrackingNumber = "SS2026040620173",
        CustomerId = UserId,
        Amount = 200,
        PaymentMethod = PaymentMethod.Online,
        PaymentStatus = PaymentStatus.Pending,
        RazorpayOrderId = OrderId,
        CreatedAt = DateTime.UtcNow
    };

    private VerifyPaymentRequest ValidRequest() => new()
    {
        RazorpayOrderId = OrderId,
        ShipmentId = ShipmentId,
        RazorpayPaymentId = "pay_MOCK_789",
        Signature = "sig_MOCK_ABC"
    };

    private void SetupSaga() =>
        _sagaRepo.Setup(r => r.GetByShipmentIdAsync(ShipmentId))
                 .ReturnsAsync(new ShipmentSagaCorrelation { ShipmentId = ShipmentId, CorrelationId = _correlationId });

    private void SetupSave()
    {
        _paymentRepo.Setup(r => r.Update(It.IsAny<ShipmentPayment>()));
        _uow.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
    }

    [Fact]
    public async Task VerifyPaymentAsync_ValidRequest_ReturnsSuccessResponse()
    {
        _paymentRepo.Setup(r => r.GetByOrderAndShipmentAsync(OrderId, ShipmentId)).ReturnsAsync(DefaultPayment());
        SetupSave(); SetupSaga();

        var result = await BuildService().VerifyPaymentAsync(ValidRequest());

        result.PaymentStatus.Should().Be("Paid");
        result.Message.Should().Be("Payment successful!");
    }

    [Fact]
    public async Task VerifyPaymentAsync_ValidRequest_MarksPaymentAsPaid()
    {
        var payment = DefaultPayment();
        _paymentRepo.Setup(r => r.GetByOrderAndShipmentAsync(OrderId, ShipmentId)).ReturnsAsync(payment);
        SetupSave(); SetupSaga();

        await BuildService().VerifyPaymentAsync(ValidRequest());

        payment.PaymentStatus.Should().Be(PaymentStatus.Paid);
        payment.RazorpayPaymentId.Should().Be("pay_MOCK_789");
        payment.RazorpaySignature.Should().Be("sig_MOCK_ABC");
        payment.PaidAt.Should().NotBeNull();
    }

    [Fact]
    public async Task VerifyPaymentAsync_ValidRequest_PublishesPaymentCompletedEvent()
    {
        _paymentRepo.Setup(r => r.GetByOrderAndShipmentAsync(OrderId, ShipmentId)).ReturnsAsync(DefaultPayment());
        SetupSave(); SetupSaga();

        await BuildService().VerifyPaymentAsync(ValidRequest());

        _publisher.WasPublished<PaymentCompletedEvent>().Should().BeTrue();
        var evt = _publisher.GetPublished<PaymentCompletedEvent>();
        evt!.ShipmentId.Should().Be(ShipmentId);
        evt.CorrelationId.Should().Be(_correlationId);
        evt.PaymentMethod.Should().Be("Online");
        evt.PaymentStatus.Should().Be("Paid");
    }

    [Fact]
    public async Task VerifyPaymentAsync_InvalidOrderId_ThrowsKeyNotFoundException()
    {
        _paymentRepo.Setup(r => r.GetByOrderAndShipmentAsync(It.IsAny<string>(), It.IsAny<int?>())).ReturnsAsync((ShipmentPayment?)null);
        _paymentRepo.Setup(r => r.GetByShipmentIdAsync(ShipmentId)).ReturnsAsync(DefaultPayment());
        SetupSave(); SetupSaga();

        var act = async () => await BuildService().VerifyPaymentAsync(new VerifyPaymentRequest
        { RazorpayOrderId = "order_WRONG", ShipmentId = ShipmentId });

        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("*Invalid Order ID*");
    }

    [Fact]
    public async Task VerifyPaymentAsync_InvalidOrderId_MarksPaymentAsFailed()
    {
        var existing = DefaultPayment();
        _paymentRepo.Setup(r => r.GetByOrderAndShipmentAsync(It.IsAny<string>(), It.IsAny<int?>())).ReturnsAsync((ShipmentPayment?)null);
        _paymentRepo.Setup(r => r.GetByShipmentIdAsync(ShipmentId)).ReturnsAsync(existing);
        SetupSave(); SetupSaga();

        try { await BuildService().VerifyPaymentAsync(new VerifyPaymentRequest { RazorpayOrderId = "order_WRONG", ShipmentId = ShipmentId }); }
        catch (KeyNotFoundException) { }

        existing.PaymentStatus.Should().Be(PaymentStatus.Failed);
        _uow.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task VerifyPaymentAsync_InvalidOrderId_PublishesPaymentFailedEvent()
    {
        _paymentRepo.Setup(r => r.GetByOrderAndShipmentAsync(It.IsAny<string>(), It.IsAny<int?>())).ReturnsAsync((ShipmentPayment?)null);
        _paymentRepo.Setup(r => r.GetByShipmentIdAsync(ShipmentId)).ReturnsAsync(DefaultPayment());
        SetupSave(); SetupSaga();

        try { await BuildService().VerifyPaymentAsync(new VerifyPaymentRequest { RazorpayOrderId = "order_WRONG", ShipmentId = ShipmentId }); }
        catch (KeyNotFoundException) { }

        _publisher.WasPublished<PaymentFailedEvent>().Should().BeTrue();
        var evt = _publisher.GetPublished<PaymentFailedEvent>();
        evt!.ShipmentId.Should().Be(ShipmentId);
        evt.CorrelationId.Should().Be(_correlationId);
        evt.Reason.Should().Contain("order_WRONG");
    }

    [Fact]
    public async Task VerifyPaymentAsync_InvalidOrderId_StillPublishesEvent_WhenNoSagaCorrelation()
    {
        _paymentRepo.Setup(r => r.GetByOrderAndShipmentAsync(It.IsAny<string>(), It.IsAny<int?>())).ReturnsAsync((ShipmentPayment?)null);
        _paymentRepo.Setup(r => r.GetByShipmentIdAsync(ShipmentId)).ReturnsAsync(DefaultPayment());
        _paymentRepo.Setup(r => r.Update(It.IsAny<ShipmentPayment>()));
        _uow.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _sagaRepo.Setup(r => r.GetByShipmentIdAsync(It.IsAny<int>())).ReturnsAsync((ShipmentSagaCorrelation?)null);

        try { await BuildService().VerifyPaymentAsync(new VerifyPaymentRequest { RazorpayOrderId = "order_WRONG", ShipmentId = ShipmentId }); }
        catch (KeyNotFoundException) { }

        _publisher.WasPublished<PaymentFailedEvent>().Should().BeTrue();
        _publisher.GetPublished<PaymentFailedEvent>()!.CorrelationId.Should().Be(Guid.Empty);
    }

    [Fact]
    public async Task VerifyPaymentAsync_WrongOwner_ThrowsUnauthorizedAccessException()
    {
        var payment = DefaultPayment();
        payment.CustomerId = 99;
        _paymentRepo.Setup(r => r.GetByOrderAndShipmentAsync(OrderId, ShipmentId)).ReturnsAsync(payment);

        var act = async () => await BuildService().VerifyPaymentAsync(ValidRequest());
        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("*not authorized*");
    }

    [Fact]
    public async Task VerifyPaymentAsync_AlreadyPaid_ThrowsInvalidOperationException()
    {
        var payment = DefaultPayment();
        payment.PaymentStatus = PaymentStatus.Paid;
        _paymentRepo.Setup(r => r.GetByOrderAndShipmentAsync(OrderId, ShipmentId)).ReturnsAsync(payment);

        var act = async () => await BuildService().VerifyPaymentAsync(ValidRequest());
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*already verified*");
    }

    [Fact]
    public async Task VerifyPaymentAsync_NoToken_ThrowsUnauthorizedAccessException()
    {
        var service = new PaymentSvc(
            _paymentRepo.Object, _sagaRepo.Object, _uow.Object, _publisher,
            NullLogger<PaymentSvc>.Instance,
            MockHttpClientFactory.WithNotFound(),
            MockHttpContext.Unauthenticated());

        var act = async () => await service.VerifyPaymentAsync(ValidRequest());
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }
}