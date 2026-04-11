using FluentAssertions;
using Moq;
using SmartShip.Shared.Events;
using SmartShip.ShipmentService.Domain.Enums;
using SmartShip.ShipmentService.Tests.Infrastructure;

namespace SmartShip.ShipmentService.Tests.UnitTests.Services;

public class ShipmentServiceTests_SchedulePickup : ShipmentServiceTestBase
{
    private static SchedulePickupRequest MakePickupRequest(int daysFromNow = 1) => new()
    {
        PickupTime = DateTime.Now.AddDays(daysFromNow)
    };

    [Fact]
    public async Task SchedulePickupAsync_CODPaid_SetsStatusToBooked()
    {
        var shipment = MakeShipment(status: ShipmentStatus.Draft);
        ShipmentRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(shipment);
        UnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var paymentResponse = new { PaymentStatus = "Paid", PaymentMethod = "COD" };
        var httpFactory = MockHttpClientFactory.WithResponse(paymentResponse);
        var svc = BuildService(httpFactory);

        await svc.SchedulePickupAsync(1, MakePickupRequest());

        shipment.Status.Should().Be(ShipmentStatus.Booked);
        shipment.PickupScheduledAt.Should().NotBeNull();
    }

    [Fact]
    public async Task SchedulePickupAsync_OnlinePaid_SetsStatusToBooked()
    {
        var shipment = MakeShipment(status: ShipmentStatus.Draft);
        ShipmentRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(shipment);
        UnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var paymentResponse = new { PaymentStatus = "Paid", PaymentMethod = "Online" };
        var httpFactory = MockHttpClientFactory.WithResponse(paymentResponse);
        var svc = BuildService(httpFactory);

        await svc.SchedulePickupAsync(1, MakePickupRequest());

        shipment.Status.Should().Be(ShipmentStatus.Booked);
    }

    [Fact]
    public async Task SchedulePickupAsync_OnlinePending_ThrowsInvalidOperation()
    {
        var shipment = MakeShipment(status: ShipmentStatus.Draft);
        ShipmentRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(shipment);

        var paymentResponse = new { PaymentStatus = "Pending", PaymentMethod = "Online" };
        var httpFactory = MockHttpClientFactory.WithResponse(paymentResponse);
        var svc = BuildService(httpFactory);

        var act = async () => await svc.SchedulePickupAsync(1, MakePickupRequest());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Online payment not completed*");
    }

    [Fact]
    public async Task SchedulePickupAsync_NoPaymentRecord_ThrowsInvalidOperation()
    {
        var shipment = MakeShipment(status: ShipmentStatus.Draft);
        ShipmentRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(shipment);
        var httpFactory = MockHttpClientFactory.WithNotFound();
        var svc = BuildService(httpFactory);

        var act = async () => await svc.SchedulePickupAsync(1, MakePickupRequest());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Payment not found*");
    }

    [Fact]
    public async Task SchedulePickupAsync_NotDraftStatus_ThrowsInvalidOperation()
    {
        var shipment = MakeShipment(status: ShipmentStatus.Booked);
        ShipmentRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(shipment);
        var svc = BuildService();

        var act = async () => await svc.SchedulePickupAsync(1, MakePickupRequest());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Draft*");
    }

    [Fact]
    public async Task SchedulePickupAsync_PublishesBookedStatusEvent()
    {
        var shipment = MakeShipment(status: ShipmentStatus.Draft);
        ShipmentRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(shipment);
        UnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var paymentResponse = new { PaymentStatus = "Paid", PaymentMethod = "COD" };
        var httpFactory = MockHttpClientFactory.WithResponse(paymentResponse);
        var svc = BuildService(httpFactory);

        await svc.SchedulePickupAsync(1, MakePickupRequest());

        Publisher.WasPublished<ShipmentStatusUpdatedEvent>().Should().BeTrue();
        var evt = Publisher.GetPublished<ShipmentStatusUpdatedEvent>();
        evt!.NewStatus.Should().Be("Booked");
        evt.OldStatus.Should().Be("Draft");
    }

    [Fact]
    public async Task SchedulePickupAsync_ShipmentNotFound_ThrowsKeyNotFoundException()
    {
        ShipmentRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Domain.Entities.Shipment?)null);
        var svc = BuildService();

        var act = async () => await svc.SchedulePickupAsync(999, MakePickupRequest());

        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("*999*");
    }
}