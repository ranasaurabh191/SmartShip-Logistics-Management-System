using System.ComponentModel.DataAnnotations.Schema;

namespace SmartShip.PaymentService.Domain.Entities;

public class ShipmentSagaCorrelation
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int ShipmentId { get; set; }
    public Guid CorrelationId { get; set; }
}