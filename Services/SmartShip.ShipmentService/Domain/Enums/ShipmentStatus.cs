namespace SmartShip.ShipmentService.Domain.Enums
{
    public enum ShipmentStatus
    {
        Draft, Booked, PickedUp, InTransit, OutForDelivery, Delivered,
        Delayed, Failed, Returned, Cancelled
    }
}
