# SmartShip.Shared — Shared Events Contract Library

## Overview

The **Smartship.Shared** library is a **zero-dependency, contract-only class library** that defines the shared message contracts (events and commands) used for inter-service communication across the entire SmartShip platform. It contains no business logic, no infrastructure code, and no framework dependencies — only plain C# POCO (Plain Old CLR Object) classes that represent the data contracts flowing through RabbitMQ. Every service that publishes or consumes messages via MassTransit references this library. It is the **single source of truth** for the "language" that services use to talk to each other.

---

## Overall Architecture & Design Decisions

### Pattern: Shared Contract Library (Anti-Corruption Layer Contracts)

In a microservices architecture, services communicate via messages. The critical challenge is: **how does the consumer know the shape of a message it never writes?** The solution is a shared contract library — a NuGet-like package (referenced as a project reference here) that all services include. This pattern is called **Shared Kernel** in Domain-Driven Design (DDD) terminology.

**Why a separate project and not copy-paste DTOs in each service?**  
If `ShipmentCreatedEvent` is defined independently in ShipmentService and NotificationService, any change to the event in ShipmentService would silently break NotificationService (it would consume a different schema). A single shared library guarantees that the publisher and consumer always agree on the contract.

**Trade-off of shared contracts:**
- ✅ Guarantees compile-time contract consistency.
- ⚠️ Creates a **coupling point** — all services must update together when an event schema changes. This is the #1 argument against Shared Kernel in strict microservices.

**Alternative (not implemented):** Schema registry (e.g., Confluent Schema Registry with Avro/Protobuf). Each service version independently, schemas are validated at publish time. More complex to set up, more flexible for evolution.

---

## Folder Structure

```
Smartship.Shared/
└── Events/
    ├── UserCreatedEvent.cs
    ├── UserDeletedEvent.cs
    ├── ShipmentCreatedEvent.cs
    ├── ShipmentStatusUpdatedEvent.cs
    ├── ShipmentDeliveredEvent.cs
    ├── CancelledEvent.cs               # ShipmentCancelledEvent
    ├── ShipmentCancelledByCustomerEvent.cs
    ├── PaymentCreatedEvent.cs
    ├── PaymentCompletedEvent.cs
    ├── PaymentFailedEvent.cs
    ├── PaymentRefundedEvent.cs
    └── CancelShipmentCommand.cs        # Command (not event — has imperative intent)
```

**Event vs. Command naming convention:**
- **Events** (past tense: `ShipmentCreatedEvent`) — something that happened. Publishers don't know who will consume them.
- **Commands** (imperative: `CancelShipmentCommand`) — a directive telling a specific service to do something. Here, the Saga publishes `CancelShipmentCommand` specifically for `CancelShipmentConsumer` in ShipmentService.

---

## Event Contracts — Detailed Explanation

### `UserCreatedEvent.cs`

```csharp
public class UserCreatedEvent
{
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
```

**Published by:** IdentityService (after successful user registration)  
**Consumed by:** NotificationService (welcome email), AdminService (increment customer count)

**Design note:** Email is included in this event because the user's email is available at creation time and NotificationService needs it immediately for the welcome email. This is the one case where NotificationService doesn't need to call back to IdentityService.

---

### `UserDeletedEvent.cs`

```csharp
public class UserDeletedEvent
{
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public DateTime DeletedAt { get; set; }
    public string Role { get; set; } = string.Empty;
}
```

**Published by:** IdentityService (after hard-delete)  
**Consumed by:** ShipmentService, PaymentService, NotificationService, AdminService

**Design note:** `Role` field enables AdminService to decrement `TotalCustomers` only for CUSTOMER role deletions. Without this field, AdminService would blindly decrement even when an ADMIN user is deleted.

---

### `ShipmentCreatedEvent.cs`

```csharp
public class ShipmentCreatedEvent
{
    public int ShipmentId { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public Guid CorrelationId { get; set; }    // ← SAGA COORDINATION KEY
    public string SenderCity { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public decimal Amount { get; set; }
}
```

**Published by:** ShipmentService  
**Consumed by:** Saga (ShipmentService), PaymentService (store CorrelationId), TrackingService (first tracking event), NotificationService (creation email), AdminService (increment counters)

**Critical field: `CorrelationId`**  
This GUID is generated by ShipmentService at the moment of shipment creation. It flows through ALL subsequent events, allowing the Saga to correlate `PaymentCompletedEvent` back to the exact `ShipmentOrderState` instance. If this field were missing from any event, the Saga would be unable to advance the correct workflow instance.

**`SenderCity` field:** Included because NotificationService's creation email shows "From: {SenderCity}" and TrackingService's first tracking event uses it as the initial location. Avoids a back-call to ShipmentService.

---

### `ShipmentStatusUpdatedEvent.cs`

```csharp
public class ShipmentStatusUpdatedEvent
{
    public int ShipmentId { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;
    public string OldStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
    public int CustomerId { get; set; }
}
```

**Published by:** ShipmentService (on every admin status update + pickup scheduling)  
**Consumed by:** TrackingService (tracking event), NotificationService (status update email)

**Design note:** `OldStatus` + `NewStatus` together tell the full story of a status transition — "from Booked to PickedUp." The notification email uses both. TrackingService uses `NewStatus` as the event's status label and `Location` as the geographic location.

---

### `ShipmentDeliveredEvent.cs`

```csharp
public class ShipmentDeliveredEvent
{
    public int ShipmentId { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public DateTime DeliveredAt { get; set; }
    public string? Location { get; set; }
}
```

**Published by:** ShipmentService (when admin updates status to `Delivered`)  
**Consumed by:** TrackingService, NotificationService, AdminService

**Why a separate `ShipmentDeliveredEvent` instead of just `ShipmentStatusUpdatedEvent` with `NewStatus = "Delivered"`?**  
Two reasons:
1. **Semantic clarity** — "Delivered" is a terminal state with special business meaning (triggers `DeliveredToday` counter, congratulatory email). A generic status event would require all consumers to check `if (newStatus == "Delivered")`.
2. **Different consumers** — AdminService subscribes to `ShipmentDeliveredEvent` for `DeliveredToday` counter but may not care about every status update. Separate event types allow selective subscription.

---

### `ShipmentCancelledEvent.cs` (in CancelledEvent.cs)

```csharp
public class ShipmentCancelledEvent
{
    public int ShipmentId { get; set; }
    public string TrackingNumber { get; set; } = "";
    public DateTime CancelledAt { get; set; } = DateTime.Now;
    public int CustomerId { get; set; }
}
```

**Published by:** ShipmentService (via `CancelShipmentConsumer` after receiving `CancelShipmentCommand`) or on admin cancellation  
**Consumed by:** AdminService (decrement active shipments), NotificationService (cancellation email)

---

### `ShipmentCancelledByCustomerEvent.cs`

```csharp
public class ShipmentCancelledByCustomerEvent
{
    public Guid CorrelationId { get; set; }
    public int ShipmentId { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public decimal Amount { get; set; }
    public bool WasPaid { get; set; }       // ← Drives refund logic
    public DateTime CancelledAt { get; set; } = DateTime.Now;
    public string Reason { get; set; } = string.Empty;
}
```

**Published by:** ShipmentService (when customer calls DELETE on their shipment)  
**Consumed by:** Saga (ShipmentService) + PaymentService

**`WasPaid` flag:** If `true`, PaymentService must issue a refund via Razorpay. If `false` (pre-payment cancellation), no refund needed. This boolean disambiguates the consumer's action without requiring a separate query to PaymentService.

**Why `CorrelationId` here too?** The customer might cancel a Confirmed shipment (post-payment). The Saga must also be updated to `Cancelled` state to clean up. The `CorrelationId` routes this event to the correct Saga instance.

---

### `PaymentCreatedEvent.cs`

```csharp
public class PaymentCreatedEvent
{
    public int ShipmentId { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
```

**Published by:** PaymentService (on `create-order` call)  
**Consumed by:** TrackingService (creates "PaymentInitiated" tracking event), NotificationService (sends payment initiated email)

**Note:** No `CorrelationId` here — this event is purely for notifications and tracking. It doesn't need to advance any Saga.

---

### `PaymentCompletedEvent.cs`

```csharp
public class PaymentCompletedEvent
{
    public Guid CorrelationId { get; set; }    // ← REQUIRED for Saga advancement
    public int ShipmentId { get; set; }
    public string TrackingNumber { get; set; } = "";
    public string PaymentMethod { get; set; } = "";
    public string PaymentStatus { get; set; } = "";
    public int CustomerId { get; set; }
}
```

**Published by:** PaymentService (after HMAC signature verification)  
**Consumed by:** Saga (transitions PaymentPending → Confirmed), TrackingService, NotificationService

**Why `CorrelationId` is critical here:**  
This event MUST advance the Saga from `PaymentPending` to `Confirmed`. MassTransit routes it to the correct Saga instance using `CorrelateById(ctx => ctx.Message.CorrelationId)`.

---

### `PaymentFailedEvent.cs`

```csharp
public class PaymentFailedEvent
{
    public Guid CorrelationId { get; set; }
    public int ShipmentId { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime FailedAt { get; set; }
}
```

**Published by:** PaymentService (on failed HMAC verification or payment not found)  
**Consumed by:** Saga (triggers `CancelShipmentCommand`), TrackingService, NotificationService, AdminService (increments Exceptions)

**`Reason` field:** Enables meaningful error messages in tracking events and notification emails ("Payment failed: Invalid signature").

---

### `PaymentRefundedEvent.cs`

```csharp
public class PaymentRefundedEvent
{
    public int ShipmentId { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public decimal Amount { get; set; }
    public DateTime RefundedAt { get; set; } = DateTime.Now;
}
```

**Published by:** PaymentService (when customer cancels a paid shipment)  
**Consumed by:** TrackingService (creates "Refunded" tracking event), NotificationService (refund confirmation email)

**No `CorrelationId`:** Refunds happen after the Saga is already in `Confirmed` or `Cancelled` state. No Saga advancement needed.

---

### `CancelShipmentCommand.cs`

```csharp
public class CancelShipmentCommand
{
    public Guid CorrelationId { get; set; }
    public int ShipmentId { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string Reason { get; set; } = string.Empty;
}
```

**Published by:** `ShipmentOrderStateMachine` (Saga) when `PaymentFailedEvent` arrives  
**Consumed by:** `CancelShipmentConsumer` in ShipmentService

**Why is this a Command and not an Event?**  
- Events describe facts: "Payment failed."  
- Commands are directives: "Cancel this shipment NOW."  
The Saga knows exactly who should handle this — `CancelShipmentConsumer`. Using a command creates a point-to-point channel rather than a broadcast. In MassTransit, `Publish` broadcasts; `Send` is point-to-point. Both use the same mechanism here, but the naming convention clarifies intent: "this is an instruction, not a notification."

---

## Event Flow Map — Complete System View

```
IdentityService
  ├── publishes → UserCreatedEvent
  │                ↳ NotificationService (welcome email)
  │                ↳ AdminService (TotalCustomers++)
  └── publishes → UserDeletedEvent
                   ↳ ShipmentService (delete shipments)
                   ↳ PaymentService (delete payments)
                   ↳ NotificationService (delete records)
                   ↳ AdminService (TotalCustomers--)

ShipmentService
  ├── publishes → ShipmentCreatedEvent
  │                ↳ Saga (Initial → PaymentPending)
  │                ↳ PaymentService (store CorrelationId)
  │                ↳ TrackingService (first tracking event)
  │                ↳ NotificationService (creation email)
  │                ↳ AdminService (TotalShipments++, ActiveShipments++)
  ├── publishes → ShipmentStatusUpdatedEvent
  │                ↳ TrackingService (status tracking event)
  │                ↳ NotificationService (status update email)
  ├── publishes → ShipmentDeliveredEvent
  │                ↳ TrackingService (delivered tracking event)
  │                ↳ NotificationService (delivery email)
  │                ↳ AdminService (ActiveShipments--, DeliveredToday++)
  ├── publishes → ShipmentCancelledEvent
  │                ↳ AdminService (ActiveShipments--)
  │                ↳ NotificationService (cancellation email)
  └── publishes → ShipmentCancelledByCustomerEvent
                   ↳ Saga (→ Cancelled)
                   ↳ PaymentService (if WasPaid → refund)

PaymentService
  ├── publishes → PaymentCreatedEvent
  │                ↳ TrackingService (PaymentInitiated tracking)
  │                ↳ NotificationService (payment initiated email)
  ├── publishes → PaymentCompletedEvent
  │                ↳ Saga (PaymentPending → Confirmed)
  │                ↳ TrackingService (PaymentVerified tracking)
  │                ↳ NotificationService (payment confirmed email)
  ├── publishes → PaymentFailedEvent
  │                ↳ Saga (→ Cancelled → publishes CancelShipmentCommand)
  │                ↳ TrackingService (PaymentFailed tracking)
  │                ↳ NotificationService (payment failed email)
  │                ↳ AdminService (Exceptions++)
  └── publishes → PaymentRefundedEvent
                   ↳ TrackingService (Refunded tracking)
                   ↳ NotificationService (refund email)

Saga (in ShipmentService)
  └── publishes → CancelShipmentCommand
                   ↳ CancelShipmentConsumer (ShipmentService) → ShipmentCancelledEvent
```

---

## Key Technologies

| Technology | Why Used |
|---|---|
| **.NET Class Library** | Zero-dependency; compiled into all referencing services |
| **MassTransit message types** | No MassTransit dependency needed — any POCO can be a message type |
| **C# record or class** | Regular classes used here; C# records would add value-equality |

---

## Interview-Ready Insights

### Potential Interview Questions

1. **"Why is `CorrelationId` (Guid) on some events but not others?"**
   → Only events that need to advance the Saga carry `CorrelationId`. Events purely for notifications/tracking don't need it — they're consumed by services that don't participate in the Saga.

2. **"What's the difference between `ShipmentCancelledEvent` and `ShipmentCancelledByCustomerEvent`?"**
   → `ShipmentCancelledEvent` is the result of a system cancellation (payment failure via Saga). `ShipmentCancelledByCustomerEvent` is a customer-initiated cancellation that may trigger a refund (`WasPaid` flag) and must also advance the Saga.

3. **"Why is `CancelShipmentCommand` a command and not an event?"**
   → Semantic distinction. Events are past-tense facts broadcast to all interested parties. Commands are directives to a specific consumer. The Saga explicitly targets `CancelShipmentConsumer` in ShipmentService.

4. **"Why maintain a shared library instead of using a schema registry?"**
   → For a single-team, single-language (.NET) system, a shared library provides compile-time safety — schema changes cause build failures, preventing silent schema mismatches. A schema registry (Avro/Protobuf) is better for multi-language or multi-team systems.

5. **"What happens if you add a new field to `ShipmentCreatedEvent`?"**
   → All services referencing this class see the new field. Services that don't use it simply ignore it. Removing a field or changing its type would be a **breaking change** — consuming services would fail to deserialize. Adding optional (nullable) fields is always backward-compatible.

### Potential Improvements

- **Versioned Events:** `ShipmentCreatedEventV2` alongside `ShipmentCreatedEvent` — allows gradual migration when schemas change.
- **C# Records:** Replace classes with `record` types for immutability and value-based equality — events should be immutable.
- **Interface-based Contracts:** `IHasCorrelationId` or `IHasShipmentId` interfaces would enable generic consumers.
- **NuGet Package:** Convert to an actual NuGet package for true service independence (services update their own version of the contract).
