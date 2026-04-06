using FluentAssertions;
using Moq;
using SmartShip.Shared.Events;
using SmartShip.ShipmentService.Core.DTOs;
using SmartShip.ShipmentService.Domain.Enums;
using SmartShip.ShipmentService.Tests.Infrastructure;

namespace SmartShip.ShipmentService.Tests.UnitTests.Services;

public class ShipmentServiceTests_Create : ShipmentServiceTestBase
{
    private CreateShipmentRequest MakeRequest(ShipmentType type = ShipmentType.Domestic) => new(
        SenderAddress: new AddressDto("Alice", "9000000001", "1 MG Road", "Amritsar", "Punjab", "143001", "India"),
        ReceiverAddress: new AddressDto("Bob", "9000000002", "2 Park St", "Delhi", "Delhi", "110001", "India"),
        Package: new PackageDto(2.5, 30, 20, 15, "Books", 500),
        ShipmentType: type,
        PickupScheduledAt: null,
        Notes: "Handle with care"
    );

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsShipmentResponse()
    {
        var httpFactory = MockHttpClientFactory.WithResponse(new { exists = true });
        var svc = BuildService(httpFactory);

        AddressRepo.Setup(r => r.AddRangeAsync(It.IsAny<Domain.Entities.Address>(), It.IsAny<Domain.Entities.Address>()))
            .Returns(Task.CompletedTask);
        PackageRepo.Setup(r => r.AddAsync(It.IsAny<Domain.Entities.Package>())).Returns(Task.CompletedTask);
        ShipmentRepo.Setup(r => r.AddAsync(It.IsAny<Domain.Entities.Shipment>())).Returns(Task.CompletedTask);
        UnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await svc.CreateAsync(MakeRequest(), customerId: 1);

        result.Should().NotBeNull();
        result.CustomerId.Should().Be(1);
        result.TrackingNumber.Should().StartWith("SS");
        result.Status.Should().Be("Draft");
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_PublishesShipmentCreatedEvent()
    {
        var httpFactory = MockHttpClientFactory.WithResponse(new { exists = true });
        var svc = BuildService(httpFactory);

        AddressRepo.Setup(r => r.AddRangeAsync(It.IsAny<Domain.Entities.Address>(), It.IsAny<Domain.Entities.Address>())).Returns(Task.CompletedTask);
        PackageRepo.Setup(r => r.AddAsync(It.IsAny<Domain.Entities.Package>())).Returns(Task.CompletedTask);
        ShipmentRepo.Setup(r => r.AddAsync(It.IsAny<Domain.Entities.Shipment>())).Returns(Task.CompletedTask);
        UnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        await svc.CreateAsync(MakeRequest(), customerId: 1);

        Publisher.WasPublished<ShipmentCreatedEvent>().Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_SavesCorrectCustomerAndType()
    {
        var httpFactory = MockHttpClientFactory.WithResponse(new { exists = true });
        var svc = BuildService(httpFactory);

        Domain.Entities.Shipment? saved = null;
        ShipmentRepo.Setup(r => r.AddAsync(It.IsAny<Domain.Entities.Shipment>()))
            .Callback<Domain.Entities.Shipment>(s => saved = s)
            .Returns(Task.CompletedTask);
        AddressRepo.Setup(r => r.AddRangeAsync(It.IsAny<Domain.Entities.Address>(), It.IsAny<Domain.Entities.Address>())).Returns(Task.CompletedTask);
        PackageRepo.Setup(r => r.AddAsync(It.IsAny<Domain.Entities.Package>())).Returns(Task.CompletedTask);
        UnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        await svc.CreateAsync(MakeRequest(ShipmentType.Express), customerId: 42);

        saved.Should().NotBeNull();
        saved!.CustomerId.Should().Be(42);
        saved.ShipmentType.Should().Be(ShipmentType.Express);
        saved.Status.Should().Be(ShipmentStatus.Draft);
    }

    [Fact]
    public async Task CreateAsync_CustomerNotFound_ThrowsKeyNotFoundException()
    {
        var httpFactory = MockHttpClientFactory.WithNotFound();
        var svc = BuildService(httpFactory);

        var act = async () => await svc.CreateAsync(MakeRequest(), customerId: 999);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*not exist*");
    }

    [Fact]
    public async Task CreateAsync_CalculatesCorrectRate_Domestic()
    {
        var httpFactory = MockHttpClientFactory.WithResponse(new { exists = true });
        var svc = BuildService(httpFactory);
        Domain.Entities.Shipment? saved = null;
        ShipmentRepo.Setup(r => r.AddAsync(It.IsAny<Domain.Entities.Shipment>()))
            .Callback<Domain.Entities.Shipment>(s => saved = s).Returns(Task.CompletedTask);
        AddressRepo.Setup(r => r.AddRangeAsync(It.IsAny<Domain.Entities.Address>(), It.IsAny<Domain.Entities.Address>())).Returns(Task.CompletedTask);
        PackageRepo.Setup(r => r.AddAsync(It.IsAny<Domain.Entities.Package>())).Returns(Task.CompletedTask);
        UnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        await svc.CreateAsync(MakeRequest(ShipmentType.Domestic), customerId: 1);

        saved!.ShippingRate.Should().Be(200m);
    }

    [Fact]
    public async Task CreateAsync_CalculatesMinimumRate_VeryLowWeight()
    {
        var httpFactory = MockHttpClientFactory.WithResponse(new { exists = true });
        var svc = BuildService(httpFactory);
        Domain.Entities.Shipment? saved = null;
        ShipmentRepo.Setup(r => r.AddAsync(It.IsAny<Domain.Entities.Shipment>()))
            .Callback<Domain.Entities.Shipment>(s => saved = s).Returns(Task.CompletedTask);
        AddressRepo.Setup(r => r.AddRangeAsync(It.IsAny<Domain.Entities.Address>(), It.IsAny<Domain.Entities.Address>())).Returns(Task.CompletedTask);
        PackageRepo.Setup(r => r.AddAsync(It.IsAny<Domain.Entities.Package>())).Returns(Task.CompletedTask);
        UnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var req = new CreateShipmentRequest(
            SenderAddress: new AddressDto("Alice", "9000000001", "1 MG Road", "Amritsar", "Punjab", "143001", "India"),
            ReceiverAddress: new AddressDto("Bob", "9000000002", "2 Park St", "Delhi", "Delhi", "110001", "India"),
            Package: new PackageDto(0.1, 30, 20, 15, "Books", 500),  // ← weight here
            ShipmentType: ShipmentType.Domestic,
            PickupScheduledAt: null,
            Notes: null
        );

        await svc.CreateAsync(req, customerId: 1);

        saved!.ShippingRate.Should().Be(99m);
    }
}