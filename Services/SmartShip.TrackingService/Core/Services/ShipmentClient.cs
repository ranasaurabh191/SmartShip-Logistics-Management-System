using SmartShip.TrackingService.Core.DTOs;
using SmartShip.TrackingService.Core.Interfaces.Services;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

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
        var token = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();

        _logger.LogInformation("Forwarding token: {HasToken}", !string.IsNullOrEmpty(token));

        if (!string.IsNullOrEmpty(token))
            _http.DefaultRequestHeaders.Authorization =
                AuthenticationHeaderValue.Parse(token);
    }
    public class FlexibleDateTimeConverter : JsonConverter<DateTime>
    {
        private static readonly string[] Formats =
        [
            "dd-MMM-yyyy hh:mm tt",
        "dd-MMM-yyyy HH:mm",
        "yyyy-MM-ddTHH:mm:ss",
        "yyyy-MM-dd"
        ];

        public override DateTime Read(ref Utf8JsonReader reader,
            Type typeToConvert, JsonSerializerOptions options)
        {
            var str = reader.GetString() ?? string.Empty;
            if (DateTime.TryParseExact(str, Formats,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var dt))
                return dt;
            if (DateTime.TryParse(str, out var fallback))
                return fallback;
            return DateTime.MinValue;
        }

        public override void Write(Utf8JsonWriter writer,
            DateTime value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.ToString("dd-MMM-yyyy hh:mm tt"));
    }
    public async Task<List<ShipmentSummary>> GetUserShipmentsAsync(int userId, bool isAdmin)
    {
        try
        {
            AttachToken();
            var endpoint = isAdmin
                ? "api/admin/shipments?page=1&pageSize=50"
                : "api/shipments/my?page=1&pageSize=50";

            _logger.LogInformation("Calling ShipmentService: {Endpoint}", endpoint);

            var response = await _http.GetAsync(endpoint);
            if (!response.IsSuccessStatusCode) return [];

            var raw = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new FlexibleDateTimeConverter() }
            };

            var result = JsonSerializer.Deserialize<PagedShipmentResponse>(raw, options);
            _logger.LogInformation("Deserialized {Count} shipments", result?.Data?.Count ?? 0);
       
            foreach (var s in result?.Data ?? [])
                _logger.LogInformation("  → {Tracking} Status={Status} Payment={Payment}",
                    s.TrackingNumber, s.Status, s.PaymentStatus);
            _logger.LogInformation("Deserialized {Count} shipments", result?.Data?.Count ?? 0);
            return result?.Data ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ShipmentClient failed for User {UserId}", userId);
            return [];
        }
    }

    public async Task<ShipmentSummary?> GetShipmentByIdAsync(int shipmentId)
    {
        try
        {
            AttachToken();
            var response = await _http.GetAsync($"api/shipments/{shipmentId}");
            if (!response.IsSuccessStatusCode) return null;

            var raw = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new FlexibleDateTimeConverter() }
            };
            return JsonSerializer.Deserialize<ShipmentSummary>(raw, options);
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