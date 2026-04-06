using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SmartShip.ShipmentService.Core.Interfaces.Persistence;
using SmartShip.ShipmentService.Core.Interfaces.Repositories;
using SmartShip.ShipmentService.Domain.Entities;
using SmartShip.ShipmentService.Domain.Enums;

namespace SmartShip.ShipmentService.Tests.Infrastructure;

public abstract class ShipmentServiceTestBase
{
    protected readonly Mock<IShipmentRepository> ShipmentRepo = new();
    protected readonly Mock<IAddressRepository> AddressRepo = new();
    protected readonly Mock<IPackageRepository> PackageRepo = new();
    protected readonly Mock<IShipmentOrderSagaRepository> SagaRepo = new();
    protected readonly Mock<IUnitOfWork> UnitOfWork = new();
    protected readonly MockPublishEndpoint Publisher = new();

    protected IConfiguration BuildConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Services:CustomerService"] = "http://localhost:5001",
                ["Services:PaymentService"] = "http://localhost:5003"
            })
            .Build();

    protected Core.Services.ShipmentService BuildService(
        IHttpClientFactory? httpClientFactory = null,
        int userId = 1)
    {
        var httpContext = MockHttpContext.WithUserId(userId);

        return new Core.Services.ShipmentService(
            ShipmentRepo.Object,
            AddressRepo.Object,
            PackageRepo.Object,
            SagaRepo.Object,
            UnitOfWork.Object,
            NullLogger<Core.Services.ShipmentService>.Instance,
            Publisher,
            httpClientFactory ?? MockHttpClientFactory.WithNotFound(),
            httpContext,
            BuildConfig());
    }

    protected static Shipment MakeShipment(
        int id = 1,
        int customerId = 1,
        ShipmentStatus status = ShipmentStatus.Draft,
        DateTime? pickupAt = null) => new()
        {
            Id = id,
            CustomerId = customerId,
            TrackingNumber = "SS20250101" + id,
            ShipmentType = ShipmentType.Domestic,
            Status = status,
            ShippingRate = 299,
            PickupScheduledAt = pickupAt,
            SenderAddress = new Address
            {
                FullName = "Sender",
                Phone = "9000000001",
                Street = "1 MG Road",
                City = "Amritsar",
                State = "Punjab",
                PostalCode = "143001",
                Country = "India"
            },
            ReceiverAddress = new Address
            {
                FullName = "Receiver",
                Phone = "9000000002",
                Street = "2 Park St",
                City = "Delhi",
                State = "Delhi",
                PostalCode = "110001",
                Country = "India"
            },
            Package = new Package
            {
                WeightKg = 2.5,
                LengthCm = 30,
                WidthCm = 20,
                HeightCm = 15,
                Description = "Electronics",
                DeclaredValue = 5000
            }
        };
}