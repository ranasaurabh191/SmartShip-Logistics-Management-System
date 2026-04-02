using MassTransit;
using SmartShip.PaymentService.Data;
using SmartShip.Shared.Events;
using Microsoft.EntityFrameworkCore;
using SmartShip.PaymentService.Models;
using SmartShip.PaymentService.DTOs;
namespace SmartShip.PaymentService.Messaging.Consumers;

public class ShipmentCreatedConsumer : IConsumer<ShipmentCreatedEvent>
{
    private readonly PaymentDbContext _context;
    private readonly ILogger<ShipmentCreatedConsumer> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;  

    public ShipmentCreatedConsumer(
        PaymentDbContext context,
        ILogger<ShipmentCreatedConsumer> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration config)             
    {
        _context = context;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _config = config;
    }

    public async Task Consume(ConsumeContext<ShipmentCreatedEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation("ShipmentCreatedEvent received | ShipmentId: {ShipmentId}", msg.ShipmentId);

        var httpClient = _httpClientFactory.CreateClient("ShipmentService");
        httpClient.DefaultRequestHeaders.Add("X-Internal-Key", _config["InternalApi:Key"]); 

        Guid correlationId = Guid.Empty;

        for (int attempt = 1; attempt <= 5; attempt++)
        {
            await Task.Delay(attempt * 400); 

            var response = await httpClient.GetAsync($"api/shipments/{msg.ShipmentId}/saga-correlation");

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<SagaCorrelationDto>();
                correlationId = result?.CorrelationId ?? Guid.Empty;

                if (correlationId != Guid.Empty)
                {
                    _logger.LogInformation("Got CorrelationId on attempt {Attempt}: {CorrelationId} for Shipment {ShipmentId}",
                        attempt, correlationId, msg.ShipmentId);
                    break;
                }
            }

            _logger.LogWarning("Attempt {Attempt}/5: Saga not ready for Shipment {ShipmentId}, retrying...",
                attempt, msg.ShipmentId);
        }

        if (correlationId == Guid.Empty)
            _logger.LogError("Could not get CorrelationId for Shipment {ShipmentId} after 5 attempts.", msg.ShipmentId);

        var existing = await _context.SagaCorrelations
            .FirstOrDefaultAsync(x => x.ShipmentId == msg.ShipmentId);

        if (existing == null)
        {
            _context.SagaCorrelations.Add(new ShipmentSagaCorrelation
            {
                ShipmentId = msg.ShipmentId,
                CorrelationId = correlationId
            });
        }
        else
        {
            existing.CorrelationId = correlationId;
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("CorrelationId stored for Shipment {ShipmentId}: {CorrelationId}",
            msg.ShipmentId, correlationId);
    }
}