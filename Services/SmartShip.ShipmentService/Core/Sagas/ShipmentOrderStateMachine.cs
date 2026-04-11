using MassTransit;
using SmartShip.Shared.Events;
using SmartShip.ShipmentService.Domain.Entities;

namespace SmartShip.ShipmentService.Core.Sagas;

public class ShipmentOrderStateMachine : MassTransitStateMachine<ShipmentOrderState>
{
    public State PaymentPending { get; private set; } = null!;
    public State Confirmed { get; private set; } = null!;
    public State Cancelled { get; private set; } = null!;

    public Event<ShipmentCreatedEvent> ShipmentCreated { get; private set; } = null!;
    public Event<PaymentCompletedEvent> PaymentCompleted { get; private set; } = null!;
    public Event<PaymentFailedEvent> PaymentFailed { get; private set; } = null!;
    public Event<ShipmentCancelledByCustomerEvent> ShipmentCancelledByCustomer { get; private set; } = null!;

    private readonly ILogger<ShipmentOrderStateMachine> _logger;
    public ShipmentOrderStateMachine(ILogger<ShipmentOrderStateMachine> logger)
    {
        _logger = logger;

        InstanceState(x => x.CurrentState);

        Event(() => ShipmentCreated, x =>
        {
            x.CorrelateById(ctx => ctx.Message.CorrelationId);
            x.SelectId(ctx => ctx.Message.CorrelationId);  
        });

        Event(() => PaymentCompleted,
            x => x.CorrelateById(ctx => ctx.Message.CorrelationId));

        Event(() => PaymentFailed,
            x => x.CorrelateById(ctx => ctx.Message.CorrelationId));

        Event(() => ShipmentCancelledByCustomer,
            x => x.CorrelateById(ctx => ctx.Message.CorrelationId));

        Initially(
            When(ShipmentCreated)
                .Then(ctx =>
                {
                    ctx.Saga.ShipmentId = ctx.Message.ShipmentId;
                    ctx.Saga.ShipmentIdKey = ctx.Message.ShipmentId.ToString();
                    ctx.Saga.CustomerId = ctx.Message.CustomerId;
                    ctx.Saga.TrackingNumber = ctx.Message.TrackingNumber;
                    ctx.Saga.CreatedAt = DateTime.Now;
                    ctx.Saga.Amount = ctx.Message.Amount;
                })
                .TransitionTo(PaymentPending)
        );

        During(PaymentPending,
            When(PaymentCompleted)
                .Then(ctx => ctx.Saga.UpdatedAt = DateTime.Now)
                .TransitionTo(Confirmed),

            When(PaymentFailed)
                .Then(ctx => ctx.Saga.UpdatedAt = DateTime.Now)
                .Publish(ctx => new CancelShipmentCommand
                {
                    CorrelationId = ctx.Saga.CorrelationId,
                    ShipmentId = ctx.Saga.ShipmentId,
                    TrackingNumber = ctx.Saga.TrackingNumber,
                    CustomerId = ctx.Saga.CustomerId,
                    Reason = ctx.Message.Reason
                }).TransitionTo(Cancelled),
             When(ShipmentCancelledByCustomer)
                    .TransitionTo(Cancelled)
                    .Then(ctx => _logger.LogInformation(
                        "Saga cancelled by customer (pre-payment) | ShipmentId: {ShipmentId}",
                        ctx.Saga.ShipmentId))
            );

        During(Confirmed,
                When(ShipmentCancelledByCustomer)
                    .TransitionTo(Cancelled)
                    .Then(ctx => _logger.LogInformation(
                        "Saga cancelled by customer (post-payment) | ShipmentId: {ShipmentId}",
                        ctx.Saga.ShipmentId))
        
            );

        SetCompletedWhenFinalized();
    }
}