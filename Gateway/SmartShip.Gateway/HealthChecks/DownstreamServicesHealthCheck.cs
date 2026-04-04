using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SmartShip.Gateway.HealthChecks;

public class DownstreamServicesHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DownstreamServicesHealthCheck> _logger;

    public DownstreamServicesHealthCheck(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<DownstreamServicesHealthCheck> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var services = _configuration
            .GetSection("ServiceHealthChecks")
            .GetChildren()
            .ToDictionary(x => x.Key, x => x.Value!);

        var client = _httpClientFactory.CreateClient("HealthCheckClient");
        var results = new Dictionary<string, object>();
        var unhealthy = new List<string>();
        var degraded = new List<string>();

        var tasks = services.Select(async service =>
        {
            try
            {
                var response = await client.GetAsync(service.Value, cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var serverDate = response.Headers.Date;
                    var localTime = serverDate.HasValue
                        ? serverDate.Value.ToLocalTime().ToString("dd-MMM-yyyy hh:mm:ss tt")
                        : DateTime.Now.ToString("dd-MMM-yyyy hh:mm:ss tt");

                    results[service.Key] = new
                    {
                        status = "Healthy",
                        url = service.Value,
                        statusCode = (int)response.StatusCode,
                        responseTime = localTime
                    };
                    _logger.LogDebug("{Service} is Healthy", service.Key);
                }
                else
                {
                    degraded.Add(service.Key);
                    results[service.Key] = new
                    {
                        status = "Degraded",
                        url = service.Value,
                        statusCode = (int)response.StatusCode
                    };
                    _logger.LogWarning("{Service} returned {StatusCode}", service.Key, (int)response.StatusCode);
                }
            }
            catch (HttpRequestException ex)
            {
                unhealthy.Add(service.Key);
                results[service.Key] = new
                {
                    status = "Unreachable",
                    url = service.Value,
                    error = "Service is down or not reachable",
                    detail = ex.Message
                };
                _logger.LogWarning("Health check failed for {Service}: {Error}", service.Key, ex.Message);
            }
            catch (TaskCanceledException)
            {
                unhealthy.Add(service.Key);
                results[service.Key] = new
                {
                    status = "Timeout",
                    url = service.Value,
                    error = "Health check timed out (>3s)"
                };
                _logger.LogWarning("Health check timeout for {Service}", service.Key);
            }
        });

        await Task.WhenAll(tasks);

        if (unhealthy.Count > 0)
            return HealthCheckResult.Unhealthy(
                $"Unreachable services: {string.Join(", ", unhealthy)}",
                data: results);

        if (degraded.Count > 0)
            return HealthCheckResult.Degraded(
                $"Degraded services: {string.Join(", ", degraded)}",
                data: results);

        return HealthCheckResult.Healthy("All services are healthy.", results);
    }
}