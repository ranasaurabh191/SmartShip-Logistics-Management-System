using MassTransit;

namespace SmartShip.ShipmentService.Domain.Entities;

public class ShipmentOrderState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = string.Empty;
    public int ShipmentId { get; set; }
    public string ShipmentIdKey { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public byte[]? RowVersion { get; set; }
}