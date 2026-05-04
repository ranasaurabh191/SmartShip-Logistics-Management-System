using System;

namespace SmartShip.Shared.Events
{
    public class UpdateShipmentStatusToPaymentFailedCommand
    {
        public Guid CorrelationId { get; set; }
        public int ShipmentId { get; set; }
        public string TrackingNumber { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }
}
