using MassTransit;
using Microsoft.EntityFrameworkCore;
using SmartShip.Shared.Events;
using SmartShip.ShipmentService.Core.DTOs;
using SmartShip.ShipmentService.Core.Interfaces.Persistence;
using SmartShip.ShipmentService.Core.Interfaces.Repositories;
using SmartShip.ShipmentService.Core.Interfaces.Services;
using SmartShip.ShipmentService.Domain.Entities;
using SmartShip.ShipmentService.Domain.Enums;
using SmartShip.ShipmentService.Infrastructure.Helpers;

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

            var responses = new List<ShipmentResponse>();
            foreach (var s in result.Data)
            {
                var saga = await _shipmentOrderSagaRepository.GetByShipmentIdAsync(s.Id);
                var paymentStatus = saga?.CurrentState switch
                {
                    "Confirmed" => "Paid",
                    "Cancelled" => "Cancelled",
                    "PaymentFailedState" => "Failed",
                    null => "Pending",
                    _ => "Pending"
                };
                responses.Add(MapToResponse(s, s.SenderAddress!, s.ReceiverAddress!, s.Package!, paymentStatus));
            }

            _logger.LogInformation("Fetched {Count} of {Total} shipments", result.Data.Count(), result.TotalCount);

            return new PagedResponse<ShipmentResponse>
            {
                Data = responses,
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

            var responses = new List<ShipmentResponse>();
            foreach (var s in result.Data)
            {
                var saga = await _shipmentOrderSagaRepository.GetByShipmentIdAsync(s.Id);
                var paymentStatus = saga?.CurrentState switch
                {
                    "Confirmed" => "Paid",
                    "Cancelled" => "Cancelled",
                    "PaymentFailedState" => "Failed",
                    null => "Pending",
                    _ => "Pending"
                };
                responses.Add(MapToResponse(s, s.SenderAddress!, s.ReceiverAddress!, s.Package!, paymentStatus));
            }

            _logger.LogInformation("Fetched {Count} of {Total} shipments for Customer {CustomerId}",
                result.Data.Count(), result.TotalCount, customerId);

            return new PagedResponse<ShipmentResponse>
            {
                Data = responses,
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

            var sender = MapAddress(req.SenderAddress);
            var receiver = MapAddress(req.ReceiverAddress);
            var package = MapPackage(req.Package);

            double distance = Haversine(sender.Latitude, sender.Longitude, receiver.Latitude, receiver.Longitude);
            _logger.LogInformation("Calculated distance: {Distance} km between cities", distance);

            var rate = await CalculateRateAsync(req.Package.WeightKg, req.ShipmentType, distance);
            _logger.LogInformation("Calculated shipping rate: {Rate} for Type: {Type} | Distance: {Distance}km", rate, req.ShipmentType, distance);

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
                Notes = req.Notes,
                IsFragile = req.IsFragile,
                DistanceKm = distance
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
                CorrelationId = correlationId,
                IsFragile = shipment.IsFragile
            });
            _logger.LogInformation("Shipment created Event Published.");

            return MapToResponse(shipment, sender, receiver, package, "Pending");
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
        var shipment = await _shipmentRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Shipment {id} not found.");

        var saga = await _shipmentOrderSagaRepository.GetByShipmentIdAsync(id);

        var paymentStatus = saga?.CurrentState switch
        {
            "Confirmed" => "Paid",
            "Cancelled" => "Cancelled",
            "PaymentFailedState" => "Failed",
            null => "Pending",
            _ => "Pending"
        };

        return MapToResponse(
            shipment,
            shipment.SenderAddress!,
            shipment.ReceiverAddress!,
            shipment.Package!,
            paymentStatus
        );
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

            if (st == ShipmentStatus.InTransit)
                throw new InvalidOperationException("In Transit status is managed automatically when you advance to a hub.");

            if (st == ShipmentStatus.OutForDelivery)
                throw new InvalidOperationException("Out For Delivery status is managed automatically when the final hub is reached.");

            if (st == s.Status)
                return; 

          
            if (st == ShipmentStatus.PickedUp && s.Status != ShipmentStatus.Booked)
                throw new InvalidOperationException($"Shipment must be Booked before PickedUp. Current: {s.Status}");

            if (st == ShipmentStatus.Delivered && s.Status != ShipmentStatus.OutForDelivery)
                throw new InvalidOperationException($"Shipment must be OutForDelivery before Delivered. Current: {s.Status}");

            if (st == ShipmentStatus.Booked && s.PickupScheduledAt == null)
                throw new InvalidOperationException("Cannot book shipment without scheduling pickup first.");

            var oldStatus = s.Status;
            s.Status = st;
            s.UpdatedAt = DateTime.Now;
            if (st == ShipmentStatus.Delivered)
                s.DeliveredAt = DateTime.Now;

            _shipmentRepository.Update(s);
            await _unitOfWork.SaveChangesAsync();

            if (st == ShipmentStatus.PickedUp)
            {
                var ctx = _unitOfWork.GetDbContext<Infrastructure.Data.ShipmentDbContext>();
                var existingRoute = await ctx.Set<ShipmentRoute>().AnyAsync(r => r.ShipmentId == s.Id);
                if (!existingRoute)
                {
                    _logger.LogInformation("Shipment {TrackingNumber} picked up. Generating route plan now...", s.TrackingNumber);
                    await GenerateRouteForShipmentAsync(s, s.SenderAddress!, s.ReceiverAddress!);
                }
            }

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

            if (s.Status != ShipmentStatus.Draft && s.Status != ShipmentStatus.PaymentFailed)
                throw new InvalidOperationException(
                    $"Pickup can only be scheduled for Draft or PaymentFailed shipments. Current status: {s.Status}.");

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
            TrackingNumber = s.TrackingNumber,
            Status = s.Status.ToString(),
            ShippingRate = s.ShippingRate
        });
    }

    public async Task<AdminSummaryDto> GetAdminSummaryAsync()
    {
        var shipments = await _shipmentRepository.GetAllAsync();
        
        return new AdminSummaryDto
        {
            TotalShipments = shipments.Count(),
            TotalRevenue = shipments
                .Where(s => s.PaymentMethod == PaymentMethod.Online && s.Status != ShipmentStatus.Cancelled)
                .Sum(s => s.ShippingRate),
            InTransitCount = shipments.Count(s => new[] { ShipmentStatus.Booked, ShipmentStatus.PickedUp, ShipmentStatus.InTransit, ShipmentStatus.OutForDelivery }.Contains(s.Status)),
            DeliveredCount = shipments.Count(s => s.Status == ShipmentStatus.Delivered),
            CancelledCount = shipments.Count(s => s.Status == ShipmentStatus.Cancelled)
        };
    }
    public Task<decimal> CalculateRateAsync(double weightKg, ShipmentType type, double distanceKm = 0)
    {
        decimal rate = type switch
        {
            ShipmentType.Express => (decimal)(weightKg * 150),
            ShipmentType.International => (decimal)(weightKg * 300),
            ShipmentType.Freight => (decimal)(weightKg * 50),
            ShipmentType.Domestic => (decimal)(weightKg * 80),
            _ => (decimal)(weightKg * 80)
        };

        const decimal baseDistance = 2000m;
        const decimal flatDistanceSurcharge = 200m;

        if (distanceKm > (double)baseDistance)
        {
            rate += flatDistanceSurcharge;

            _logger.LogInformation("Added flat distance surcharge: {Charge} for shipment over {BaseKm}km",
                flatDistanceSurcharge,
                baseDistance
            );
        }

        var finalRate = Math.Max(rate, 99);

        _logger.LogInformation("Rate calculated: {Rate} | Type: {Type} | Weight: {Weight}kg ",
            finalRate, type, weightKg);

        return Task.FromResult(finalRate);
    }
    public async Task<ShipmentResponse?> GetByTrackingNumberAsync(string trackingNumber)
    {
        if (string.IsNullOrWhiteSpace(trackingNumber))
            return null;

        var shipment = await _shipmentRepository.GetByTrackingNumberAsync(trackingNumber);

        if (shipment == null)
            return null;

        return MapToResponse(
            shipment,
            shipment.SenderAddress!,
            shipment.ReceiverAddress!,
            shipment.Package!
        );
    }

    public async Task<IEnumerable<RouteStopDto>> GetRouteAsync(int shipmentId)
    {
        var ctx = _unitOfWork.GetDbContext<Infrastructure.Data.ShipmentDbContext>();
        var routes = await ctx.Set<ShipmentRoute>()
            .Where(r => r.ShipmentId == shipmentId)
            .OrderBy(r => r.SequenceOrder)
            .ToListAsync();

        if (routes.Count == 0)
        {
            var shipment = await _shipmentRepository.GetByIdAsync(shipmentId);
            if (shipment != null && shipment.SenderAddress != null && shipment.ReceiverAddress != null)
            {
                await GenerateRouteForShipmentAsync(shipment, shipment.SenderAddress, shipment.ReceiverAddress);
                routes = await ctx.Set<ShipmentRoute>()
                    .Where(r => r.ShipmentId == shipmentId)
                    .OrderBy(r => r.SequenceOrder)
                    .ToListAsync();
            }
        }

        return routes.Select(r => new RouteStopDto(
            r.Id, r.ShipmentId, r.HubId, r.HubName, r.HubCity,
            r.Latitude, r.Longitude, r.SequenceOrder, r.IsCompleted, r.ReachedAt));
    }

    public async Task<RouteStopDto> AdvanceToNextHubAsync(int shipmentId)
    {
        var shipment = await _shipmentRepository.GetByIdAsync(shipmentId)
            ?? throw new KeyNotFoundException($"Shipment {shipmentId} not found.");

        var ctx = _unitOfWork.GetDbContext<Infrastructure.Data.ShipmentDbContext>();
        var routes = await ctx.Set<ShipmentRoute>()
            .Where(r => r.ShipmentId == shipmentId)
            .OrderBy(r => r.SequenceOrder)
            .ToListAsync();

        if (routes.Count == 0)
            throw new InvalidOperationException("No route plan exists for this shipment.");

        var nextStop = routes.FirstOrDefault(r => !r.IsCompleted);
        if (nextStop == null)
            throw new InvalidOperationException("All hubs in the route have been completed.");

        var oldStatus = shipment.Status;
        nextStop.IsCompleted = true;
        nextStop.ReachedAt = DateTime.Now;

        var allCompleted = routes.All(r => r.IsCompleted);
        if (allCompleted)
        {
            shipment.Status = ShipmentStatus.OutForDelivery;
        }
        else
        {
            shipment.Status = ShipmentStatus.InTransit;
        }

        shipment.UpdatedAt = DateTime.Now;
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Shipment {TrackingNumber} advanced to hub: {Hub} (seq {Seq}). Status: {Old} -> {New}",
            shipment.TrackingNumber, nextStop.HubName, nextStop.SequenceOrder, oldStatus, shipment.Status);

        if (oldStatus != shipment.Status)
        {
            await _publisher.Publish(new ShipmentStatusUpdatedEvent
            {
                ShipmentId = shipment.Id,
                TrackingNumber = shipment.TrackingNumber,
                OldStatus = oldStatus.ToString(),
                NewStatus = shipment.Status.ToString(),
                Location = nextStop.HubCity ?? nextStop.HubName,
                UpdatedBy = "system-auto",
                UpdatedAt = DateTime.Now,
                CustomerId = shipment.CustomerId
            });
        }

        return new RouteStopDto(
            nextStop.Id, nextStop.ShipmentId, nextStop.HubId, nextStop.HubName, nextStop.HubCity!,
            nextStop.Latitude, nextStop.Longitude, nextStop.SequenceOrder, nextStop.IsCompleted, nextStop.ReachedAt);
    }

    private async Task GenerateRouteForShipmentAsync(Shipment shipment, Address sender, Address receiver)
    {
        try
        {
            var client = CreateInternalClient("AdminService");
            var response = await client.GetAsync("api/admin/hubs/all-active");
            if (!response.IsSuccessStatusCode) return;

            var hubs = await response.Content.ReadFromJsonAsync<List<HubInfo>>();
            if (hubs == null || hubs.Count == 0) return;

            var roadPoints = new List<double[]>();
            try
            {
                var osrmUrl = $"https://router.project-osrm.org/route/v1/driving/{sender.Longitude.ToString().Replace(',','.')}," +
                              $"{sender.Latitude.ToString().Replace(',','.')};{receiver.Longitude.ToString().Replace(',','.')}," +
                              $"{receiver.Latitude.ToString().Replace(',','.')}?overview=full&geometries=geojson";

                var osrmRes = await new HttpClient().GetAsync(osrmUrl);
                if (osrmRes.IsSuccessStatusCode)
                {
                    var osrmJson = await osrmRes.Content.ReadAsStringAsync();
                    var osrmData = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(osrmJson);
                    
                    if (osrmData.GetProperty("code").GetString() == "Ok")
                    {
                        var routes = osrmData.GetProperty("routes");
                        if (routes.GetArrayLength() > 0)
                        {
                            var coords = routes[0].GetProperty("geometry").GetProperty("coordinates");
                            foreach (var c in coords.EnumerateArray())
                            {
                                roadPoints.Add(new double[] { c[1].GetDouble(), c[0].GetDouble() });
                            }
                        }
                    }
                }
            }
            catch { }

            var routeStops = new List<ShipmentRoute>();
            var sequence = 0;

            if (roadPoints.Count > 10)
            {
                var indices = new int[] { 
                    (int)(roadPoints.Count * 0.2), 
                    (int)(roadPoints.Count * 0.4), 
                    (int)(roadPoints.Count * 0.6), 
                    (int)(roadPoints.Count * 0.8) 
                };

                foreach (var idx in indices)
                {
                    var p = roadPoints[idx];
                    var lat = p[0]; var lon = p[1];

                    var nearbyHub = hubs
                        .Where(h => Haversine(lat, lon, h.Latitude, h.Longitude) < 150)
                        .OrderBy(h => Haversine(lat, lon, h.Latitude, h.Longitude))
                        .FirstOrDefault();

                    if (nearbyHub != null)
                    {
                        if (routeStops.Count > 0 && routeStops.Last().HubId == nearbyHub.Id) continue;
                        
                        routeStops.Add(new ShipmentRoute {
                            ShipmentId = shipment.Id, HubId = nearbyHub.Id, HubName = nearbyHub.Name,
                            HubCity = nearbyHub.City, Latitude = nearbyHub.Latitude, Longitude = nearbyHub.Longitude,
                            SequenceOrder = sequence++, IsCompleted = false
                        });
                    }
                    else
                    {
                        string resolvedCity = "Highway Transit";
                        try {
                            var revUrl = $"https://nominatim.openstreetmap.org/reverse?format=json&lat={lat.ToString().Replace(',','.')}&lon={lon.ToString().Replace(',','.')}&zoom=10";
                            using var revClient = new HttpClient();
                            revClient.DefaultRequestHeaders.Add("User-Agent", "SmartShip-Logistics-App");
                            var revRes = await revClient.GetAsync(revUrl);
                            if (revRes.IsSuccessStatusCode) {
                                var revJson = await revRes.Content.ReadAsStringAsync();
                                var revData = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(revJson);
                                if (revData.TryGetProperty("address", out var addr)) {
                                    resolvedCity = (addr.TryGetProperty("city", out var c) ? c.GetString() : null) ??
                                                   (addr.TryGetProperty("town", out var t) ? t.GetString() : null) ??
                                                   (addr.TryGetProperty("village", out var v) ? v.GetString() : null) ??
                                                   (addr.TryGetProperty("county", out var co) ? co.GetString() : null) ??
                                                   "Highway Transit";
                                }
                            }
                        } catch { }

                        routeStops.Add(new ShipmentRoute {
                            ShipmentId = shipment.Id, HubId = null, HubName = resolvedCity + " Node",
                            HubCity = resolvedCity, Latitude = lat, Longitude = lon,
                            SequenceOrder = sequence++, IsCompleted = false
                        });
                    }
                }
            }
            else
            {
                var sHub = FindNearestHub(hubs, sender.City, sender.State, sender.Latitude, sender.Longitude);
                var rHub = FindNearestHub(hubs, receiver.City, receiver.State, receiver.Latitude, receiver.Longitude);
                if (sHub != null) routeStops.Add(new ShipmentRoute { ShipmentId = shipment.Id, HubId = sHub.Id, HubName = sHub.Name, HubCity = sHub.City, Latitude = sHub.Latitude, Longitude = sHub.Longitude, SequenceOrder = sequence++, IsCompleted = false });
                if (rHub != null && rHub.Id != sHub?.Id) routeStops.Add(new ShipmentRoute { ShipmentId = shipment.Id, HubId = rHub.Id, HubName = rHub.Name, HubCity = rHub.City, Latitude = rHub.Latitude, Longitude = rHub.Longitude, SequenceOrder = sequence++, IsCompleted = false });
            }

            var ctx = _unitOfWork.GetDbContext<Infrastructure.Data.ShipmentDbContext>();
            foreach (var stop in routeStops) ctx.Set<ShipmentRoute>().Add(stop);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Route generated for {Tracking} with {Count} road-optimized stops.",
                shipment.TrackingNumber, routeStops.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate road-optimized route for shipment {Tracking}", shipment.TrackingNumber);
        }
    }

    private static HubInfo? FindNearestHub(List<HubInfo> hubs, string city, string state, double lat = 0, double lon = 0)
    {
        var cleanCity = (city ?? "").Trim();
        var cleanState = (state ?? "").Trim();

        var cityMatch = hubs.FirstOrDefault(h =>
            h.City.Equals(cleanCity, StringComparison.OrdinalIgnoreCase) ||
            cleanCity.Contains(h.City, StringComparison.OrdinalIgnoreCase) ||
            h.City.Contains(cleanCity, StringComparison.OrdinalIgnoreCase));
        if (cityMatch != null) return cityMatch;

        if (lat != 0 && lon != 0)
        {
            return hubs
                .OrderBy(h => Haversine(lat, lon, h.Latitude, h.Longitude))
                .FirstOrDefault();
        }

        var stateMatch = hubs.FirstOrDefault(h =>
            h.State.Equals(cleanState, StringComparison.OrdinalIgnoreCase) ||
            cleanState.Contains(h.State, StringComparison.OrdinalIgnoreCase));
        if (stateMatch != null) return stateMatch;

        return hubs.FirstOrDefault(h => h.City.Equals("Bhopal", StringComparison.OrdinalIgnoreCase)) 
               ?? hubs.FirstOrDefault();
    }

    private static double Haversine(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371; // Earth radius in km
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLon = (lon2 - lon1) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
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
        Country = d.Country,
        Latitude = d.Latitude ?? 0,
        Longitude = d.Longitude ?? 0
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

    private static ShipmentResponse MapToResponse(Shipment s, Address sender, Address receiver, Package pkg, string paymentStatus = "Unknown") => new(
        s.Id,
        s.TrackingNumber,
        s.CustomerId,
        s.ShipmentType.ToString(),
        s.Status.ToString(),
         paymentStatus,
        s.ShippingRate,
        s.CreatedAt.ToString("dd-MMM-yyyy hh:mm tt"),
        s.PickupScheduledAt?.ToString("dd-MMM-yyyy hh:mm tt"),
        s.DeliveredAt?.ToString("dd-MMM-yyyy hh:mm tt"),
        new AddressDto(sender.FullName, sender.Phone, sender.Street, sender.City, sender.State, sender.PostalCode, sender.Country, sender.Latitude, sender.Longitude),
        new AddressDto(receiver.FullName, receiver.Phone, receiver.Street, receiver.City, receiver.State, receiver.PostalCode, receiver.Country, receiver.Latitude, receiver.Longitude),
        new PackageDto(pkg.WeightKg, pkg.LengthCm, pkg.WidthCm, pkg.HeightCm, pkg.Description, pkg.DeclaredValue),
        s.Notes,
        s.IsFragile,
        s.DistanceKm
    );
}