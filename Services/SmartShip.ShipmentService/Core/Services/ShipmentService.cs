using MassTransit;
using SmartShip.Shared.Events;
using SmartShip.ShipmentService.Domain.Entities;
using SmartShip.ShipmentService.Core.DTOs;
using SmartShip.ShipmentService.Infrastructure.Helpers;
using SmartShip.ShipmentService.Core.Interfaces.Services;
using SmartShip.ShipmentService.Core.Interfaces.Repositories;
using SmartShip.ShipmentService.Core.Interfaces.Persistence;
using SmartShip.ShipmentService.Domain.Enums;

namespace SmartShip.ShipmentService.Core.Services;

public class ShipmentService : IShipmentService
{
    private readonly IShipmentRepository _shipmentRepository;
    private readonly IAddressRepository _addressRepository;
    private readonly IPackageRepository _packageRepository;
    private readonly IShipmentOrderSagaRepository _shipmentOrderSagaRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ShipmentService> _logger;
    private readonly IPublishEndpoint _publisher;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _config;

    public ShipmentService(
        IShipmentRepository shipmentRepository,
        IAddressRepository addressRepository,
        IPackageRepository packageRepository,
        IShipmentOrderSagaRepository shipmentOrderSagaRepository,
        IUnitOfWork unitOfWork,
        ILogger<ShipmentService> logger,
        IPublishEndpoint publisher,
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration config)
    {
        _shipmentRepository = shipmentRepository;
        _addressRepository = addressRepository;
        _packageRepository = packageRepository;
        _shipmentOrderSagaRepository = shipmentOrderSagaRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _publisher = publisher;
        _httpClientFactory = httpClientFactory;
        _httpContextAccessor = httpContextAccessor;
        _config = config;
    }

    public async Task<PagedResponse<ShipmentResponse>> GetAllPagedAsync(ShipmentPagedRequest req)
    {
        _logger.LogInformation("Fetching all shipments | Page: {Page} | PageSize: {PageSize} | Status: {Status} | Type: {Type}",
            req.Page, req.PageSize, req.Status ?? "All", req.ShipmentType ?? "All");

        try
        {
            var result = await _shipmentRepository.GetAllPagedAsync(req);

            _logger.LogInformation("Fetched {Count} of {Total} shipments", result.Data.Count(), result.TotalCount);

            return new PagedResponse<ShipmentResponse>
            {
                Data = result.Data.Select(s => MapToResponse(s, s.SenderAddress!, s.ReceiverAddress!, s.Package!)),
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch all shipments");
            throw;
        }
    }

    public async Task<PagedResponse<ShipmentResponse>> GetMyShipmentsPagedAsync(int customerId, PagedRequest req)
    {
        _logger.LogInformation("Fetching shipments for Customer {CustomerId} | Page: {Page} | PageSize: {PageSize}",
            customerId, req.Page, req.PageSize);

        try
        {
            var result = await _shipmentRepository.GetByCustomerPagedAsync(customerId, req);

            _logger.LogInformation("Fetched {Count} of {Total} shipments for Customer {CustomerId}",
                result.Data.Count(), result.TotalCount, customerId);

            return new PagedResponse<ShipmentResponse>
            {
                Data = result.Data.Select(s => MapToResponse(s, s.SenderAddress!, s.ReceiverAddress!, s.Package!)),
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch shipments for Customer {CustomerId}", customerId);
            throw;
        }
    }

    public async Task<ShipmentResponse> CreateAsync(CreateShipmentRequest req, int customerId)
    {
        _logger.LogInformation("Creating shipment for Customer {CustomerId} | Type: {Type} | Weight: {Weight}kg",
            customerId, req.ShipmentType, req.Package.WeightKg);

        try
        {
            var customerExists = await ConsumerHelper.ValidateCustomerExistsAsync(
                _httpClientFactory, _logger, customerId, _config);

            if (!customerExists)
            {
                _logger.LogWarning("Shipment creation rejected — Customer {CustomerId} not found or inactive.", customerId);
                throw new KeyNotFoundException($"Customer {customerId} does not exist or is inactive.");
            }

            _logger.LogInformation("Customer {CustomerId} validated. Proceeding with shipment creation.", customerId);

            var rate = await CalculateRateAsync(req.Package.WeightKg, req.ShipmentType);
            _logger.LogInformation("Calculated shipping rate: {Rate} for Type: {Type}", rate, req.ShipmentType);

            var sender = MapAddress(req.SenderAddress);
            var receiver = MapAddress(req.ReceiverAddress);
            var package = MapPackage(req.Package);

            await _addressRepository.AddRangeAsync(sender, receiver);
            await _packageRepository.AddAsync(package);
            await _unitOfWork.SaveChangesAsync();

            var shipment = new Shipment
            {
                TrackingNumber = GenerateTrackingNumber(),
                CustomerId = customerId,
                ShipmentType = req.ShipmentType,
                Status = ShipmentStatus.Draft,
                ShippingRate = rate,
                SenderAddressId = sender.Id,
                ReceiverAddressId = receiver.Id,
                PackageId = package.Id,
                PickupScheduledAt = req.PickupScheduledAt,
                Notes = req.Notes
            };

            shipment.SenderAddress = sender;
            shipment.ReceiverAddress = receiver;
            shipment.Package = package;

            var correlationId = NewId.NextSequentialGuid();

            await _shipmentRepository.AddAsync(shipment);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Shipment created: {TrackingNumber} | Rate: {Rate} | Customer: {CustomerId}",
                shipment.TrackingNumber, rate, customerId);

            await _publisher.Publish(new ShipmentCreatedEvent
            {
                ShipmentId = shipment.Id,
                TrackingNumber = shipment.TrackingNumber,
                CustomerId = shipment.CustomerId,
                SenderCity = sender.City,
                CreatedAt = shipment.CreatedAt,
                Amount = shipment.ShippingRate,
                CorrelationId = correlationId
            });
            _logger.LogInformation("Shipment created Event Published.");
            return MapToResponse(shipment, sender, receiver, package);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create shipment for Customer {CustomerId}", customerId);
            throw;
        }
    }

    public async Task CancelByCustomerAsync(int shipmentId, int customerId, string reason)
    {
        var shipment = await _shipmentRepository.GetByIdAndCustomerAsync(shipmentId, customerId);

        if (shipment == null)
            throw new KeyNotFoundException("Shipment not found.");

        if (shipment.Status != ShipmentStatus.Draft && shipment.Status != ShipmentStatus.Booked)
        {
            throw new InvalidOperationException(
                $"Shipment cannot be cancelled. Current status: {shipment.Status}. Only Draft or Booked shipments can be cancelled.");
        }

        bool wasPaid = shipment.Status == ShipmentStatus.Booked;

        var saga = await _shipmentOrderSagaRepository.GetByShipmentIdAsync(shipmentId);
        var correlationId = saga?.CorrelationId ?? Guid.Empty;

        shipment.Status = ShipmentStatus.Cancelled;
        shipment.Notes = $"Cancelled by customer: {reason}";
        shipment.UpdatedAt = DateTime.Now;

        _shipmentRepository.Update(shipment);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Shipment {TrackingNumber} cancelled by Customer {CustomerId} | WasPaid: {WasPaid}",
            shipment.TrackingNumber, customerId, wasPaid);

        await _publisher.Publish(new ShipmentCancelledByCustomerEvent
        {
            CorrelationId = correlationId,
            ShipmentId = shipment.Id,
            TrackingNumber = shipment.TrackingNumber,
            CustomerId = customerId,
            Amount = shipment.ShippingRate,
            WasPaid = wasPaid,
            CancelledAt = DateTime.Now,
            Reason = reason
        });

        await _publisher.Publish(new ShipmentCancelledEvent
        {
            ShipmentId = shipment.Id,
            TrackingNumber = shipment.TrackingNumber,
            CustomerId = customerId,
            CancelledAt = DateTime.Now
        });

        _logger.LogInformation("ShipmentCancelledByCustomerEvent published for {TrackingNumber}", shipment.TrackingNumber);
    }

    public async Task<ShipmentResponse> GetByIdAsync(int id)
    {
        _logger.LogInformation("Fetching shipment by ID: {ShipmentId}", id);

        var s = await _shipmentRepository.GetByIdWithDetailsAsync(id);

        if (s == null)
        {
            _logger.LogWarning("Shipment not found: ID {ShipmentId}", id);
            throw new KeyNotFoundException($"Shipment {id} not found.");
        }

        _logger.LogInformation("Shipment found: {TrackingNumber} | Status: {Status}", s.TrackingNumber, s.Status);

        return MapToResponse(s, s.SenderAddress!, s.ReceiverAddress!, s.Package!);
    }

    public async Task UpdateStatusAsync(int id, UpdateStatusRequest request)
    {
        _logger.LogInformation("Updating status for Shipment {ShipmentId} -> {Status}", id, request.Status);

        try
        {
            var s = await _shipmentRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Shipment {id} not found.");

            if (!Enum.TryParse<ShipmentStatus>(request.Status, true, out var st))
            {
                _logger.LogWarning("Invalid status value: {Status}", request.Status);
                throw new ArgumentException($"Invalid status: {request.Status}");
            }

            if (st == ShipmentStatus.Cancelled && s.Status == ShipmentStatus.Delivered)
                throw new InvalidOperationException("Cannot cancel a delivered shipment.");

            if (st == ShipmentStatus.PickedUp && s.Status != ShipmentStatus.Booked)
                throw new InvalidOperationException("Shipment must be Booked before PickedUp.");

            if (st == ShipmentStatus.InTransit && s.Status != ShipmentStatus.PickedUp)
                throw new InvalidOperationException("Shipment must be PickedUp before InTransit.");

            if (st == ShipmentStatus.OutForDelivery && s.Status != ShipmentStatus.InTransit)
                throw new InvalidOperationException("Shipment must be InTransit before OutForDelivery.");

            if (st == ShipmentStatus.Delivered && s.Status != ShipmentStatus.OutForDelivery)
                throw new InvalidOperationException("Shipment must be OutForDelivery before Delivered.");

            if (st == ShipmentStatus.Booked && s.PickupScheduledAt == null)
                throw new InvalidOperationException("Cannot book shipment without scheduling pickup first.");

            var oldStatus = s.Status;
            s.Status = st;
            s.UpdatedAt = DateTime.Now;
            if (st == ShipmentStatus.Delivered)
                s.DeliveredAt = DateTime.Now;

            _shipmentRepository.Update(s);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Shipment {TrackingNumber} status: {OldStatus} → {NewStatus}",
                s.TrackingNumber, oldStatus, st);

            if (st is not (ShipmentStatus.Booked or ShipmentStatus.Delivered))
            {
                await _publisher.Publish(new ShipmentStatusUpdatedEvent
                {
                    ShipmentId = s.Id,
                    TrackingNumber = s.TrackingNumber,
                    OldStatus = oldStatus.ToString(),
                    NewStatus = s.Status.ToString(),
                    Location = request.Location ?? "Unknown Hub",
                    UpdatedBy = "Agent-" + DateTime.Now.ToString("hhmm"),
                    UpdatedAt = DateTime.Now,
                    CustomerId = s.CustomerId
                });
            }

            if (s.Status == ShipmentStatus.Delivered)
            {
                _logger.LogInformation("Publishing ShipmentDeliveredEvent for {TrackingNumber}", s.TrackingNumber);

                await _publisher.Publish(new ShipmentDeliveredEvent
                {
                    ShipmentId = s.Id,
                    TrackingNumber = s.TrackingNumber,
                    Location = request.Location ?? "Customer Address",
                    CustomerId = s.CustomerId,
                    DeliveredAt = DateTime.Now
                });
            }

            if (s.Status == ShipmentStatus.Cancelled)
            {
                _logger.LogInformation("Publishing ShipmentCancelledEvent for {TrackingNumber}", s.TrackingNumber);

                await _publisher.Publish(new ShipmentCancelledEvent
                {
                    ShipmentId = s.Id,
                    TrackingNumber = s.TrackingNumber,
                    CancelledAt = DateTime.Now,
                    CustomerId = s.CustomerId
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update status for Shipment {ShipmentId}", id);
            throw;
        }
    }

    public async Task SchedulePickupAsync(int id, int customerId, SchedulePickupRequest request)
    {
        _logger.LogInformation("Scheduling pickup for Shipment {ShipmentId} | Customer {CustomerId}", id, customerId);

        try
        {
            var s = await _shipmentRepository.GetByIdAndCustomerAsync(id, customerId);

            if (s == null)
            {
                _logger.LogWarning("Shipment {ShipmentId} not found or does not belong to Customer {CustomerId}",
                    id, customerId);
                throw new KeyNotFoundException("Shipment not found or you are not authorized to schedule pickup for it.");
            }

            if (s.Status != ShipmentStatus.Draft)
                throw new InvalidOperationException(
                    $"Pickup can only be scheduled for Draft shipments. Current status: {s.Status}.");

            var httpClient = CreateInternalClient("PaymentService");
            var response = await httpClient.GetAsync($"api/payment/shipment/{id}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("No payment record for Shipment {ShipmentId}", id);
                throw new InvalidOperationException("Payment not found. Please create a payment order first.");
            }

            var payment = await response.Content.ReadFromJsonAsync<PaymentStatusDto>();

            if (payment?.PaymentStatus == "Pending" && payment?.PaymentMethod == "Online")
            {
                _logger.LogWarning("Online payment pending for Shipment {ShipmentId}", id);
                throw new InvalidOperationException("Online payment not completed. Please pay before scheduling pickup.");
            }

            s.PickupScheduledAt = request.PickupTime;
            s.Status = ShipmentStatus.Booked;
            s.UpdatedAt = request.PickupTime;

            _shipmentRepository.Update(s);
            await _unitOfWork.SaveChangesAsync();

            await _publisher.Publish(new ShipmentStatusUpdatedEvent
            {
                ShipmentId = s.Id,
                TrackingNumber = s.TrackingNumber,
                OldStatus = "Draft",
                NewStatus = "Booked",
                Location = s.SenderAddress?.City ?? "Warehouse",
                UpdatedBy = "system",
                UpdatedAt = DateTime.Now,
                CustomerId = s.CustomerId
            });

            _logger.LogInformation("Pickup scheduled for {TrackingNumber} at {PickupTime} | Customer {CustomerId}",
                s.TrackingNumber, request.PickupTime, customerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to schedule pickup for Shipment {ShipmentId}", id);
            throw;
        }
    }

    public async Task ResolveExceptionAsync(int id, string resolution)
    {
        _logger.LogInformation("Resolving exception for Shipment {ShipmentId}", id);

        try
        {
            var s = await _shipmentRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Shipment {id} not found.");

            s.Notes = resolution;
            s.Status = ShipmentStatus.InTransit;

            _shipmentRepository.Update(s);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Exception resolved for {TrackingNumber} | Resolution: {Resolution}",
                s.TrackingNumber, resolution);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve exception for Shipment {ShipmentId}", id);
            throw;
        }
    }
    public async Task<IEnumerable<ShipmentSummaryDto>> GetShipmentSummaryByCustomerAsync(int customerId)
    {
        var shipments = await _shipmentRepository.GetByCustomerIdAsync(customerId);

        return shipments.Select(s => new ShipmentSummaryDto
        {
            Id = s.Id,
            TrackingNumber = s.TrackingNumber
        });
    }
    public Task<decimal> CalculateRateAsync(double weightKg, ShipmentType type)
    {
        decimal rate = type switch
        {
            ShipmentType.Express => (decimal)(weightKg * 150),
            ShipmentType.International => (decimal)(weightKg * 300),
            ShipmentType.Freight => (decimal)(weightKg * 50),
            ShipmentType.Domestic => (decimal)(weightKg * 80),
            _ => (decimal)(weightKg * 80)
        };

        var finalRate = Math.Max(rate, 99);

        _logger.LogInformation("Rate calculated: {Rate} | Type: {Type} | Weight: {Weight}kg",
            finalRate, type, weightKg);

        return Task.FromResult(finalRate);
    }

    private HttpClient CreateInternalClient(string clientName)
    {
        var httpClient = _httpClientFactory.CreateClient(clientName);
        var token = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();

        if (!string.IsNullOrEmpty(token))
            httpClient.DefaultRequestHeaders.Add("Authorization", token);

        return httpClient;
    }

    private static string GenerateTrackingNumber() =>
        "SS" + DateTime.Now.ToString("yyyyMMdd") + Random.Shared.Next(10000, 99999);

    private static Address MapAddress(AddressDto d) => new()
    {
        FullName = d.FullName,
        Phone = d.Phone,
        Street = d.Street,
        City = d.City,
        State = d.State,
        PostalCode = d.PostalCode,
        Country = d.Country
    };

    private static Package MapPackage(PackageDto d) => new()
    {
        WeightKg = d.WeightKg,
        LengthCm = d.LengthCm,
        WidthCm = d.WidthCm,
        HeightCm = d.HeightCm,
        Description = d.Description,
        DeclaredValue = d.DeclaredValue
    };

    private static ShipmentResponse MapToResponse(Shipment s, Address sender, Address receiver, Package pkg) => new(
        s.Id,
        s.TrackingNumber,
        s.CustomerId,
        s.ShipmentType.ToString(),
        s.Status.ToString(),
        s.ShippingRate,
        s.CreatedAt.ToString("dd-MMM-yyyy hh:mm tt"),
        s.PickupScheduledAt?.ToString("dd-MMM-yyyy hh:mm tt"),
        s.DeliveredAt?.ToString("dd-MMM-yyyy hh:mm tt"),
        new AddressDto(sender.FullName, sender.Phone, sender.Street, sender.City, sender.State, sender.PostalCode, sender.Country),
        new AddressDto(receiver.FullName, receiver.Phone, receiver.Street, receiver.City, receiver.State, receiver.PostalCode, receiver.Country),
        new PackageDto(pkg.WeightKg, pkg.LengthCm, pkg.WidthCm, pkg.HeightCm, pkg.Description, pkg.DeclaredValue),
        s.Notes
    );
}