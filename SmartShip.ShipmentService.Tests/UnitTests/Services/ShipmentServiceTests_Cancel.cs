using FluentAssertions;
using Moq;
using SmartShip.Shared.Events;
using SmartShip.ShipmentService.Domain.Entities;
using SmartShip.ShipmentService.Domain.Enums;
using SmartShip.ShipmentService.Tests.Infrastructure;

namespace SmartShip.ShipmentService.Tests.UnitTests.Services;

public class ShipmentServiceTests_Cancel : ShipmentServiceTestBase
{
    [Fact]
    public async Task CancelByCustomerAsync_DraftShipment_CancelsSuccessfully()
    {
        var shipment = MakeShipment(status: ShipmentStatus.Draft);
        ShipmentRepo.Setup(r => r.GetByIdAndCustomerAsync(1, 1)).ReturnsAsync(shipment);
        SagaRepo.Setup(r => r.GetByShipmentIdAsync(1)).ReturnsAsync((ShipmentOrderState?)null);
        UnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        var svc = BuildService();

        await svc.CancelByCustomerAsync(1, 1, "Changed my mind");

        shipment.Status.Should().Be(ShipmentStatus.Cancelled);
    }

    [Fact]
    public async Task CancelByCustomerAsync_BookedShipment_SetsWasPaidTrue()
    {
        var shipment = MakeShipment(status: ShipmentStatus.Booked, pickupAt: DateTime.UtcNow.AddDays(1));
        ShipmentRepo.Setup(r => r.GetByIdAndCustomerAsync(1, 1)).ReturnsAsync(shipment);
        SagaRepo.Setup(r => r.GetByShipmentIdAsync(1)).ReturnsAsync((ShipmentOrderState?)null);
        UnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        var svc = BuildService();

        await svc.CancelByCustomerAsync(1, 1, "Refund please");

        var evt = Publisher.GetPublished<ShipmentCancelledByCustomerEvent>();
        evt.Should().NotBeNull();
        evt!.WasPaid.Should().BeTrue();
    }

    [Fact]
    public async Task CancelByCustomerAsync_PublishesBothCancelEvents()
    {
        var shipment = MakeShipment(status: ShipmentStatus.Draft);
        ShipmentRepo.Setup(r => r.GetByIdAndCustomerAsync(1, 1)).ReturnsAsync(shipment);
        SagaRepo.Setup(r => r.GetByShipmentIdAsync(1)).ReturnsAsync((ShipmentOrderState?)null);
        UnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        var svc = BuildService();

        await svc.CancelByCustomerAsync(1, 1, "Test reason");

        Publisher.WasPublished<ShipmentCancelledByCustomerEvent>().Should().BeTrue();
        Publisher.WasPublished<ShipmentCancelledEvent>().Should().BeTrue();
    }

    [Fact]
    public async Task CancelByCustomerAsync_InTransitShipment_ThrowsInvalidOperation()
    {
        var shipment = MakeShipment(status: ShipmentStatus.InTransit);
        ShipmentRepo.Setup(r => r.GetByIdAndCustomerAsync(1, 1)).ReturnsAsync(shipment);
        var svc = BuildService();

        var act = async () => await svc.CancelByCustomerAsync(1, 1, "too late");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*cannot be cancelled*");
    }

    [Fact]
    public async Task CancelByCustomerAsync_ShipmentNotFound_ThrowsKeyNotFoundException()
    {
        ShipmentRepo.Setup(r => r.GetByIdAndCustomerAsync(999, 1)).ReturnsAsync((Domain.Entities.Shipment?)null);
        var svc = BuildService();

        var act = async () => await svc.CancelByCustomerAsync(999, 1, "test");

        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("*not found*");
    }

    [Fact]
    public async Task CancelByCustomerAsync_WithSagaCorrelation_UsesCorrectCorrelationId()
    {
        var shipment = MakeShipment(status: ShipmentStatus.Draft);
        var correlationId = Guid.NewGuid();

        var saga = new ShipmentOrderState { ShipmentId = 1, CorrelationId = correlationId };
        ShipmentRepo.Setup(r => r.GetByIdAndCustomerAsync(1, 1)).ReturnsAsync(shipment);
        SagaRepo.Setup(r => r.GetByShipmentIdAsync(1)).ReturnsAsync(saga);
        UnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        var svc = BuildService();

        await svc.CancelByCustomerAsync(1, 1, "reason");

        var evt = Publisher.GetPublished<ShipmentCancelledByCustomerEvent>();
        evt!.CorrelationId.Should().Be(correlationId);
    }
}