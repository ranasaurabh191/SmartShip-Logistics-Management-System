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

    public ShipmentCreatedConsumer(
        PaymentDbContext context,
        ILogger<ShipmentCreatedConsumer> logger,
        IHttpClientFactory httpClientFactory)               
    {
        _context = context;
        _logger = logger;
        _httpClientFactory = httpClientFactory;          
    }

    public async Task Consume(ConsumeContext<ShipmentCreatedEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation("ShipmentCreatedEvent received | ShipmentId: {ShipmentId}", msg.ShipmentId);

        await Task.Delay(500); 

        var httpClient = _httpClientFactory.CreateClient("ShipmentService");
        var response = await httpClient.GetAsync($"api/shipments/{msg.ShipmentId}/saga-correlation");

        Guid correlationId = Guid.Empty;

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<SagaCorrelationDto>();
            correlationId = result?.CorrelationId ?? Guid.Empty;
            _logger.LogInformation("Fetched real CorrelationId: {CorrelationId} for Shipment {ShipmentId}",
                correlationId, msg.ShipmentId);
        }
        else
        {
            _logger.LogWarning("Could not fetch CorrelationId for Shipment {ShipmentId}", msg.ShipmentId);
        }

        var existing = await _context.SagaCorrelations.FirstOrDefaultAsync(x => x.ShipmentId == msg.ShipmentId);

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
        _logger.LogInformation("CorrelationId stored for Shipment {ShipmentId}: {CorrelationId}", msg.ShipmentId, correlationId);
    }
}