using FluentAssertions;
using Moq;
using SmartShip.PaymentService.Core.DTOs;
using SmartShip.PaymentService.Domain.Entities;
using SmartShip.PaymentService.Domain.Entities.Enums;
using SmartShip.PaymentService.Tests.Helpers;
using SmartShip.Shared.Events;
namespace SmartShip.PaymentService.Tests.UnitTests.Services;

public class PaymentServiceTests_CreateOrder : PaymentServiceTestBase
{
    private const int ShipmentId = 26;
    private readonly Guid _correlationId = Guid.NewGuid();


    private ShipmentDTOs DefaultShipment() => new()
    {
        Id = ShipmentId,
        TrackingNumber = "SS2026040620173",
        CustomerId = UserId,
        ShippingRate = 200,
    };

    private void SetupSaga() =>
        _sagaRepo.Setup(r => r.GetByShipmentIdAsync(ShipmentId))
                 .ReturnsAsync(new ShipmentSagaCorrelation
                 {
                     ShipmentId = ShipmentId,
                     CorrelationId = _correlationId
                 });

    private void SetupNoExistingPayment() =>
        _paymentRepo.Setup(r => r.GetByShipmentIdAsync(ShipmentId))
                    .ReturnsAsync((ShipmentPayment?)null);

    private void SetupSave()
    {
        _paymentRepo.Setup(r => r.AddAsync(It.IsAny<ShipmentPayment>())).Returns(Task.CompletedTask);
        _uow.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
    }

    [Fact]
    public async Task CreateOrderAsync_Online_ReturnsSuccessResponse()
    {
        SetupNoExistingPayment(); SetupSave(); SetupSaga();
        var service = BuildService(MockHttpClientFactory.WithResponse(DefaultShipment()));

        var result = await service.CreateOrderAsync(new CreateOrderRequest(ShipmentId, PaymentMethod.Online));

        result.PaymentMethod.Should().Be("Online");
        result.PaymentStatus.Should().Be("Pending");
        result.RazorpayOrderId.Should().StartWith("order_MOCK_");
        result.TrackingNumber.Should().Be("SS2026040620173");
    }

    [Fact]
    public async Task CreateOrderAsync_Online_PublishesOnlyPaymentCreatedEvent()
    {
        SetupNoExistingPayment(); SetupSave(); SetupSaga();
        var service = BuildService(MockHttpClientFactory.WithResponse(DefaultShipment()));

        await service.CreateOrderAsync(new CreateOrderRequest(ShipmentId, PaymentMethod.Online));
        _publisher.WasPublished<PaymentCreatedEvent>().Should().BeTrue();
        _publisher.WasPublished<PaymentCompletedEvent>().Should().BeFalse();
    }

    [Fact]
    public async Task CreateOrderAsync_Online_SavesCorrectPaymentEntity()
    {
        ShipmentPayment? saved = null;
        SetupNoExistingPayment();
        SetupSaga();
        _paymentRepo.Setup(r => r.AddAsync(It.IsAny<ShipmentPayment>()))
                    .Callback<ShipmentPayment>(p => saved = p)
                    .Returns(Task.CompletedTask);
        _uow.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var service = BuildService(MockHttpClientFactory.WithResponse(DefaultShipment()));
        await service.CreateOrderAsync(new CreateOrderRequest(ShipmentId, PaymentMethod.Online));

        saved!.ShipmentId.Should().Be(ShipmentId);
        saved.CustomerId.Should().Be(UserId);
        saved.Amount.Should().Be(200);
        saved.PaymentStatus.Should().Be(PaymentStatus.Pending);
        saved.SagaCorrelationId.Should().Be(_correlationId);
    }

    [Fact]
    public async Task CreateOrderAsync_COD_PublishesBothEvents()
    {
        _publisher.Reset();
        SetupNoExistingPayment(); SetupSave(); SetupSaga();
        var service = BuildService(MockHttpClientFactory.WithResponse(DefaultShipment()));

        await service.CreateOrderAsync(new CreateOrderRequest(ShipmentId, PaymentMethod.COD));

        _publisher.WasPublished<PaymentCreatedEvent>().Should().BeTrue();
        _publisher.WasPublished<PaymentCompletedEvent>().Should().BeTrue();

        var evt = _publisher.GetPublished<PaymentCompletedEvent>();
        evt!.PaymentMethod.Should().Be("COD");
        evt.CorrelationId.Should().Be(_correlationId);
    }

    [Fact]
    public async Task CreateOrderAsync_ShipmentNotFound_ThrowsKeyNotFoundException()
    {
        var act = async () => await BuildService(MockHttpClientFactory.WithNotFound())
            .CreateOrderAsync(new CreateOrderRequest(ShipmentId, PaymentMethod.Online));

        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("*Shipment not found*");
    }

    [Fact]
    public async Task CreateOrderAsync_WrongOwner_ThrowsUnauthorizedAccessException()
    {
        var shipment = DefaultShipment();
        shipment.CustomerId = 99;

        var act = async () => await BuildService(MockHttpClientFactory.WithResponse(shipment))
            .CreateOrderAsync(new CreateOrderRequest(ShipmentId, PaymentMethod.Online));

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("*not authorized*");
    }

    [Fact]
    public async Task CreateOrderAsync_AlreadyPaid_ThrowsInvalidOperationException()
    {
        _paymentRepo.Setup(r => r.GetByShipmentIdAsync(ShipmentId))
                    .ReturnsAsync(new ShipmentPayment { PaymentStatus = PaymentStatus.Paid });

        var act = async () => await BuildService(MockHttpClientFactory.WithResponse(DefaultShipment()))
            .CreateOrderAsync(new CreateOrderRequest(ShipmentId, PaymentMethod.Online));

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*already paid*");
    }

    [Fact]
    public async Task CreateOrderAsync_CODAlreadyRegistered_ThrowsInvalidOperationException()
    {
        _paymentRepo.Setup(r => r.GetByShipmentIdAsync(ShipmentId))
                    .ReturnsAsync(new ShipmentPayment { PaymentMethod = PaymentMethod.COD, PaymentStatus = PaymentStatus.Pending });

        var act = async () => await BuildService(MockHttpClientFactory.WithResponse(DefaultShipment()))
            .CreateOrderAsync(new CreateOrderRequest(ShipmentId, PaymentMethod.Online));

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*COD*");
    }

    [Fact]
    public async Task CreateOrderAsync_OnlineAlreadyInitiated_ThrowsInvalidOperationException()
    {
        _paymentRepo.Setup(r => r.GetByShipmentIdAsync(ShipmentId))
                    .ReturnsAsync(new ShipmentPayment { PaymentMethod = PaymentMethod.Online, PaymentStatus = PaymentStatus.Pending });

        var act = async () => await BuildService(MockHttpClientFactory.WithResponse(DefaultShipment()))
            .CreateOrderAsync(new CreateOrderRequest(ShipmentId, PaymentMethod.Online));

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*already initiated*");
    }

    [Fact]
    public async Task CreateOrderAsync_NoToken_ThrowsUnauthorizedAccessException()
    {
        var service = BuildUnauthenticatedService(MockHttpClientFactory.WithResponse(DefaultShipment()));

        var act = async () => await service.CreateOrderAsync(new CreateOrderRequest(ShipmentId, PaymentMethod.Online));

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }
   
}