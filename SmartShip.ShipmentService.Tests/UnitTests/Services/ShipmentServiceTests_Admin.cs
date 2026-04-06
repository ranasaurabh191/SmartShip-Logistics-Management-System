using FluentAssertions;
using Moq;
using SmartShip.ShipmentService.Domain.Enums;
using SmartShip.ShipmentService.Tests.Infrastructure;

namespace SmartShip.ShipmentService.Tests.UnitTests.Services;

public class ShipmentServiceTests_Admin : ShipmentServiceTestBase
{
    [Fact]
    public async Task ResolveExceptionAsync_ValidShipment_SetsInTransit()
    {
        var shipment = MakeShipment(status: ShipmentStatus.Booked);
        ShipmentRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(shipment);
        UnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        var svc = BuildService();

        await svc.ResolveExceptionAsync(1, "Package re-routed via Delhi hub.");

        shipment.Status.Should().Be(ShipmentStatus.InTransit);
        shipment.Notes.Should().Be("Package re-routed via Delhi hub.");
    }

    [Fact]
    public async Task ResolveExceptionAsync_NotFound_ThrowsKeyNotFoundException()
    {
        ShipmentRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Domain.Entities.Shipment?)null);
        var svc = BuildService();

        var act = async () => await svc.ResolveExceptionAsync(999, "Resolved");

        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("*999*");
    }

    [Fact]
    public async Task CalculateRateAsync_Express_ReturnsCorrectRate()
    {
        var svc = BuildService();
        var rate = await svc.CalculateRateAsync(2.0, ShipmentType.Express);
        rate.Should().Be(300m); 
    }

    [Fact]
    public async Task CalculateRateAsync_International_ReturnsCorrectRate()
    {
        var svc = BuildService();
        var rate = await svc.CalculateRateAsync(1.0, ShipmentType.International);
        rate.Should().Be(300m); 
    }

    [Fact]
    public async Task CalculateRateAsync_Freight_ReturnsCorrectRate()
    {
        var svc = BuildService();
        var rate = await svc.CalculateRateAsync(10.0, ShipmentType.Freight);
        rate.Should().Be(500m);
    }

    [Fact]
    public async Task CalculateRateAsync_BelowMinimum_ReturnsMinimum99()
    {
        var svc = BuildService();
        var rate = await svc.CalculateRateAsync(0.01, ShipmentType.Domestic);
        rate.Should().Be(99m);
    }
}