using FluentAssertions;
using Moq;
using SmartShip.Shared.Events;
using SmartShip.ShipmentService.Core.DTOs;
using SmartShip.ShipmentService.Domain.Enums;
using SmartShip.ShipmentService.Tests.Infrastructure;
using Xunit;

namespace SmartShip.ShipmentService.Tests.UnitTests.Services;

public class ShipmentServiceTests_Status : ShipmentServiceTestBase
{
    [Fact]
    public async Task GetByIdAsync_ValidId_ReturnsResponse()
    {
        var shipment = MakeShipment(id: 5);
        ShipmentRepo.Setup(r => r.GetByIdWithDetailsAsync(5)).ReturnsAsync(shipment);
        var svc = BuildService();

        var result = await svc.GetByIdAsync(5);

        result.Should().NotBeNull();
        result.Id.Should().Be(5);
        result.TrackingNumber.Should().Be(shipment.TrackingNumber);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ThrowsKeyNotFoundException()
    {
        ShipmentRepo.Setup(r => r.GetByIdWithDetailsAsync(999)).ReturnsAsync((Domain.Entities.Shipment?)null);
        var svc = BuildService();

        var act = async () => await svc.GetByIdAsync(999);

        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("*999*");
    }

    [Fact]
    public async Task UpdateStatusAsync_ValidTransition_UpdatesStatus()
    {
        var shipment = MakeShipment(status: ShipmentStatus.PickedUp);
        ShipmentRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(shipment);
        UnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        var svc = BuildService();

        await svc.UpdateStatusAsync(1, new UpdateStatusRequest { Status = "InTransit" });

        shipment.Status.Should().Be(ShipmentStatus.InTransit);
    }

    [Fact]
    public async Task UpdateStatusAsync_Delivered_SetsDeliveredAt()
    {
        var shipment = MakeShipment(status: ShipmentStatus.OutForDelivery);
        ShipmentRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(shipment);
        UnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        var svc = BuildService();

        await svc.UpdateStatusAsync(1, new UpdateStatusRequest { Status = "Delivered" });

        shipment.DeliveredAt.Should().NotBeNull();
        Publisher.WasPublished<ShipmentDeliveredEvent>().Should().BeTrue();
    }

    [Fact]
    public async Task UpdateStatusAsync_InvalidTransition_ThrowsInvalidOperation()
    {
        var shipment = MakeShipment(status: ShipmentStatus.Draft);
        ShipmentRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(shipment);
        var svc = BuildService();

        var act = async () => await svc.UpdateStatusAsync(1, new UpdateStatusRequest { Status = "InTransit" });

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task UpdateStatusAsync_CancelDelivered_ThrowsInvalidOperation()
    {
        var shipment = MakeShipment(status: ShipmentStatus.Delivered);
        ShipmentRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(shipment);
        var svc = BuildService();

        var act = async () => await svc.UpdateStatusAsync(1, new UpdateStatusRequest { Status = "Cancelled" });

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*cannot cancel a delivered*");
    }

    [Fact]
    public async Task UpdateStatusAsync_InTransit_PublishesStatusUpdatedEvent()
    {
        var shipment = MakeShipment(status: ShipmentStatus.PickedUp);
        ShipmentRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(shipment);
        UnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        var svc = BuildService();

        await svc.UpdateStatusAsync(1, new UpdateStatusRequest { Status = "InTransit", Location = "Delhi Hub" });

        Publisher.WasPublished<ShipmentStatusUpdatedEvent>().Should().BeTrue();
        var evt = Publisher.GetPublished<ShipmentStatusUpdatedEvent>();
        evt!.NewStatus.Should().Be("InTransit");
        evt.Location.Should().Be("Delhi Hub");
    }

    [Fact]
    public async Task UpdateStatusAsync_Cancelled_PublishesCancelledEvent()
    {
        var shipment = MakeShipment(status: ShipmentStatus.Booked, pickupAt: DateTime.Now.AddDays(1));
        ShipmentRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(shipment);
        UnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        var svc = BuildService();

        await svc.UpdateStatusAsync(1, new UpdateStatusRequest { Status = "Cancelled" });

        Publisher.WasPublished<ShipmentCancelledEvent>().Should().BeTrue();
    }

    [Fact]
    public async Task UpdateStatusAsync_InvalidStatusString_ThrowsArgumentException()
    {
        var shipment = MakeShipment(status: ShipmentStatus.Draft);
        ShipmentRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(shipment);
        var svc = BuildService();

        var act = async () => await svc.UpdateStatusAsync(1, new UpdateStatusRequest { Status = "FLYING" });

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Invalid status*");
    }
}