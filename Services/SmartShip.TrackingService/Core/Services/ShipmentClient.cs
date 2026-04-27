using System.Net.Http.Headers;
using SmartShip.TrackingService.Core.DTOs;
using SmartShip.TrackingService.Core.Interfaces.Services;

namespace SmartShip.TrackingService.Core.Services;

public class ShipmentClient : IShipmentClient
{
    private readonly HttpClient _http;
    private readonly ILogger<ShipmentClient> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ShipmentClient(HttpClient http, ILogger<ShipmentClient> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _http = http;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    private void AttachToken()
    {
        var token = _httpContextAccessor.HttpContext?
            .Request.Headers["Authorization"].ToString();
        if (!string.IsNullOrEmpty(token))
            _http.DefaultRequestHeaders.Authorization =
                AuthenticationHeaderValue.Parse(token);
    }

    public async Task<List<ShipmentSummary>> GetUserShipmentsAsync(int userId, bool isAdmin)
    {
        try
        {
            AttachToken();
            var endpoint = isAdmin
                ? "api/shipments?page=1&pageSize=50"
                : "api/shipments/my?page=1&pageSize=50";

            var result = await _http.GetFromJsonAsync<PagedShipmentResponse>(endpoint);
            return result?.Data ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch shipments for User {UserId}", userId);
            return [];
        }
    }

    public async Task<ShipmentSummary?> GetShipmentByIdAsync(int shipmentId)
    {
        try
        {
            AttachToken();
            return await _http.GetFromJsonAsync<ShipmentSummary>(
                $"api/shipments/{shipmentId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch shipment {ShipmentId}", shipmentId);
            return null;
        }
    }

    private class PagedShipmentResponse
    {
        public List<ShipmentSummary> Data { get; set; } = [];
    }
}