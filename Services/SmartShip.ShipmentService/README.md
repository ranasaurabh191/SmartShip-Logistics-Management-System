# SmartShip.ShipmentService — Shipment Management Service

## Overview

The **ShipmentService** is the operational core of the SmartShip platform. It handles the entire lifecycle of a logistics shipment: creation, pickup scheduling, real-time status transitions, admin-level overrides, and customer-side tracking. Beyond CRUD operations, it hosts the **distributed Saga Orchestrator** — a `MassTransitStateMachine` that coordinates the shipment order workflow across ShipmentService and PaymentService using RabbitMQ events. It owns three domain tables (`Shipments`, `Addresses`, `Packages`) and one Saga state table (`ShipmentOrderSagas`). It runs on **port 5002** and communicates bidirectionally with PaymentService (via HTTP for payment status checks) and IdentityService (via HTTP for user validation). All significant state changes trigger events published to RabbitMQ, creating a reactive event bus across the platform.

---

## Overall Architecture & Design Decisions

### Architecture Pattern: Layered Architecture + Saga Orchestration

```
API Layer           → Controllers + ExceptionMiddleware
Core Layer          → IShipmentService + ShipmentService (business logic)
                       ShipmentOrderStateMachine (distributed saga)
                       DTOs + FluentValidation Validators
Domain Layer        → Shipment, Address, Package, ShipmentOrderState entities + Enums
Infrastructure Layer → ShipmentDbContext (EF Core) + Repositories + UnitOfWork
                       Messaging/Consumers (MassTransit)
```

**Why include the Saga in the ShipmentService?**  
The Saga manages the shipment order workflow — it's intrinsically a shipment concern (what happens to a shipment when payment succeeds/fails?). Placing it in a dedicated service would add unnecessary network hops. The ShipmentService owns the shipment state machine because it owns the `Shipment` entity.

**Communication style:**
- **Inbound REST:** Customer/admin HTTP calls via Gateway.
- **Outbound REST:** HTTP calls to PaymentService (`/api/payment/shipment/{id}`) to check payment status before scheduling pickup.
- **Outbound Events:** Publishes `ShipmentCreatedEvent`, `ShipmentStatusUpdatedEvent`, `ShipmentDeliveredEvent`, `ShipmentCancelledEvent`, `ShipmentCancelledByCustomerEvent` to RabbitMQ.
- **Inbound Commands:** Consumes `CancelShipmentCommand` (from Saga on payment failure) and `UserDeletedEvent` (for cascade cleanup).
- **Saga Messages:** `ShipmentCreatedEvent` → `PaymentCompleted/Failed/CancelledByCustomer` events coordinate state transitions.

---

## Folder Structure

```
SmartShip.ShipmentService/
├── API/
│   ├── Controllers/
│   │   ├── ShipmentController.cs       # Customer shipment operations (CRUD, pickup, cancel)
│   │   └── AdminShipmentController.cs  # Admin operations (paginated list, status override)
│   └── Middleware/
│       └── ExceptionMiddleware.cs      # Unified exception → HTTP status code mapping
├── Core/
│   ├── DTOs/                           # All request/response DTOs
│   │   ├── CreateShipmentRequest.cs    # Nested: SenderAddress + ReceiverAddress + Package + ShipmentType
│   │   ├── ShipmentResponse.cs         # Full shipment with embedded address/package DTOs
│   │   ├── UpdateStatusRequest.cs      # { Status, Location, UpdatedBy }
│   │   ├── SchedulePickupRequest.cs    # { PickupTime, Notes }
│   │   └── PagedRequest.cs             # Generic pagination request DTO
│   ├── Interfaces/
│   │   ├── Repositories/               # IShipmentRepository, IAddressRepository, IPackageRepository, IShipmentOrderSagaRepository
│   │   ├── Services/IShipmentService.cs
│   │   └── Persistence/IUnitOfWork.cs
│   ├── Sagas/
│   │   └── ShipmentOrderStateMachine.cs  # MassTransit saga orchestrator
│   ├── Services/
│   │   └── ShipmentService.cs          # All business logic: create, update, cancel, pickup, rate calculation
│   └── Validators/
│       └── ShipmentValidators.cs       # FluentValidation: Address, Package, CreateShipment, SchedulePickup
├── Domain/
│   ├── Entities/
│   │   ├── Shipment.cs                 # Main aggregate with FK to Address, Package
│   │   ├── Address.cs                  # Sender/receiver address value object
│   │   ├── Package.cs                  # Package dimensions + declared value
│   │   └── ShipmentOrderState.cs       # Saga state machine persistence class
│   └── Enums/
│       ├── ShipmentStatus.cs           # Draft, Booked, PickedUp, InTransit, OutForDelivery, Delivered, Delayed, Failed, Returned, Cancelled
│       └── ShipmentType.cs             # Domestic, International, Express, Freight
├── Infrastructure/
│   ├── Data/ShipmentDbContext.cs       # EF Core context with saga state table config
│   ├── Messaging/Consumers/
│   │   ├── CancelShipmentConsumer.cs   # Handles CancelShipmentCommand from saga
│   │   └── UserDeletedConsumer.cs      # Cascade-delete shipments on user deletion
│   ├── Persistence/UnitOfWork.cs
│   └── Repositories/
│       ├── ShipmentRepository.cs       # Complex paginated queries with eager loading
│       ├── AddressRepository.cs        # Bulk insert (AddRangeAsync)
│       ├── PackageRepository.cs
│       └── ShipmentOrderSagaRepository.cs  # Saga state lookup by shipment ID
├── Program.cs                          # Composition root with Saga + RabbitMQ + JWT config
└── appsettings.json
```

---

## API Endpoints / Message Consumers

### `POST /api/shipments` — Create Shipment

**Auth:** Bearer JWT (CUSTOMER or ADMIN role)

**Request:**
```json
{
  "shipmentType": "Domestic",
  "senderAddress": {
    "fullName": "Saurabh Rana",
    "phone": "9876543210",
    "street": "123 MG Road",
    "city": "Mumbai",
    "state": "Maharashtra",
    "postalCode": "400001",
    "country": "India"
  },
  "receiverAddress": { ... },
  "package": {
    "weightKg": 2.5,
    "lengthCm": 30,
    "widthCm": 20,
    "heightCm": 15,
    "description": "Electronics",
    "declaredValue": 15000
  },
  "pickupScheduledAt": "2026-04-20T10:00:00",
  "notes": "Handle with care"
}
```

**Business logic (step-by-step):**
1. FluentValidation validates address fields (city letters-only, 6-digit postal code, 10-digit phone), package dims (>0, weight ≤500kg, dims ≤300cm), pickup time must be in the future (if provided).
2. Extract `customerId` from JWT claim `ClaimTypes.NameIdentifier`.
3. Verify customer exists: HTTP GET to IdentityService `/api/auth/internal/user-exists/{customerId}` using `X-Internal-Key`.
4. Generate unique tracking number: `$"SS{DateTime.Now:yyyyMMddHHmmss}{new Random().Next(1000, 9999)}"`.
5. Calculate shipping rate using `CalculateRate()`:
   - Volumetric weight = `(L × W × H) / 5000`
   - Billable weight = `Max(actualWeight, volumetricWeight)`
   - Base rate per kg varies by `ShipmentType`: Domestic=50, Express=100, International=150, Freight=30.
   - Rate = `billableWeight × ratePerKg + 50 (handling fee)`.
6. Create `Address` × 2 (sender + receiver), `Package`, `Shipment` entities and persist all atomically via UoW.
7. Generate `CorrelationId = Guid.NewGuid()` for the Saga.
8. Publish `ShipmentCreatedEvent` to RabbitMQ → Saga starts, TrackingService creates first tracking event, NotificationService sends confirmation email.
9. Return `201 Created` with full `ShipmentResponse`.

**Why generate the tracking number in the service and not the DB?** DB sequences work, but application-generated tracking numbers allow custom formats (prefix "SS") and avoid a round-trip just to get an ID before insert.

**Why `CorrelationId` in the event?** This is the Saga's correlation key. Every subsequent payment event must carry this same GUID to route to the correct Saga instance. Without it, the payment for shipment #123 could accidentally advance the Saga for shipment #456.

---

### `GET /api/shipments/{id}` — Get Shipment by ID

**Auth:** Bearer JWT (any role, but customer only sees their own)

**Business logic:**
1. Extract `customerId` from JWT + check `role`.
2. If CUSTOMER: fetch with `GetByIdAndCustomerAsync(id, customerId)` — enforces row-level security (customers cannot view other customers' shipments).
3. If ADMIN: `GetByIdWithDetailsAsync(id)` — no ownership filter.
4. Eager loading with `.Include(s => s.SenderAddress).Include(s => s.ReceiverAddress).Include(s => s.Package)` — single query fetching all related data.

**Why eager loading vs. lazy loading?**  
Lazy loading causes N+1 query problems. With EF Core explicit `.Include()`, one JOIN query retrieves shipment + 2 addresses + 1 package. Lazy loading would fire 4 separate queries.

---

### `GET /api/shipments/my` — Customer's Own Shipments (Paginated)

**Auth:** Bearer JWT (CUSTOMER)

**Business logic:**
Paginated query filtered by `customerId` from JWT. Supports `?search=SS20260401&page=1&pageSize=10`.

---

### `PATCH /api/shipments/{id}/schedule-pickup` — Schedule Pickup

**Purpose:** Critical business endpoint. A customer confirms the pickup time after payment. This is the point where a shipment transitions from `Draft` to `Booked`.

**Business logic (step-by-step):**
1. Fetch shipment by ID + customerId (ownership check).
2. Validate `Status == Draft` → throw `InvalidOperationException` if already booked.
3. HTTP GET to PaymentService `/api/payment/shipment/{id}`:
   - If `PaymentMethod == "Online"` → `PaymentStatus` MUST be `"Paid"` (online payments verified before pickup).
   - If `PaymentMethod == "COD"` → no payment check needed (money collected on delivery).
   - If payment record not found → reject.
4. Update `Status = Booked`, `PickupScheduledAt = request.PickupTime`.
5. SaveChangesAsync.
6. Publish `ShipmentStatusUpdatedEvent` (`OldStatus: Draft`, `NewStatus: Booked`).

**This is one of the most interview-worthy endpoints** because it demonstrates:
- Cross-service HTTP communication
- Business rule enforcement (online payment must be paid before pickup)
- COD vs Online payment mode differentiation
- Status machine guards (can't schedule pickup if already booked)

---

### `PATCH /api/admin/shipments/{id}/status` — Update Shipment Status (Admin)

**Purpose:** Admin/operations team manually advances shipment through logistics stages.

**Valid status transitions (enforced in `ShipmentService.cs`):**
```
Draft          → only via schedule-pickup endpoint
Booked         → PickedUp, Cancelled
PickedUp       → InTransit, Cancelled
InTransit      → OutForDelivery, Delayed, Cancelled
OutForDelivery → Delivered, Failed, Returned
Delivered      → (terminal — cannot be cancelled)
Delayed/Failed → InTransit, Returned, Cancelled
Returned       → (terminal)
Cancelled      → (terminal)
```

**Business logic:**
1. Parse new status string → `ShipmentStatus` enum (argument exception if unrecognized).
2. Validate allowed transition — throw `InvalidOperationException` if illegal.
3. If `Delivered` → set `DeliveredAt = DateTime.Now` → publish `ShipmentDeliveredEvent`.
4. If `Cancelled` (and was Booked with pickup) → publish `ShipmentCancelledEvent`.
5. If any other transition → publish `ShipmentStatusUpdatedEvent`.

**Why enforce transitions in code?** Without guards, an admin could set status backwards (e.g., Delivered → Draft), corrupting business logic. Think of it as a finite state machine — the transitions ARE the business rules.

---

### `DELETE /api/shipments/{id}` — Customer Cancel Shipment

**Purpose:** Customer-initiated cancellation. If the shipment was already paid for, a refund is triggered.

**Business logic:**
1. Ownership check (customer can only cancel their own).
2. If `Status == Delivered || Cancelled` → reject.
3. Check if paid: HTTP GET to PaymentService.
4. Set `Status = Cancelled`, `Notes = reason`.
5. Publish `ShipmentCancelledByCustomerEvent` with `WasPaid = true/false`.
   - If WasPaid → PaymentService issues a refund and publishes `PaymentRefundedEvent`.

---

### Message Consumer: `CancelShipmentConsumer`

**Queue:** `shipment-cancel-command`  
**Message:** `CancelShipmentCommand`  
**Triggered by:** The `ShipmentOrderStateMachine` when `PaymentFailedEvent` is received.

**Business logic:**
1. Fetch shipment by ID.
2. Only cancel if `Status == Draft` (if already PickedUp, don't touch it — manual resolution needed).
3. Set `Status = Cancelled`, `Notes = $"Auto-cancelled: {reason}"`.
4. Publish `ShipmentCancelledEvent` → NotificationService sends cancellation email.

**Why guard `Status == Draft`?** If a shipment was already picked up but payment failed (unlikely but possible edge case), automatic cancellation would be wrong — operations team needs to handle it manually.

---

### Message Consumer: `UserDeletedConsumer`

**Queue:** `shipment-user-deleted`  
**Message:** `UserDeletedEvent`

**Business logic:**
- Hard-delete all `Shipments` where `CustomerId == deletedUserId`.
- Hard-delete all `ShipmentOrderSagas` for this customer.
- Log counts for audit.

---

## The ShipmentOrderStateMachine (Saga) — Deep Dive

This is the most architecturally sophisticated component in the entire codebase, and the most likely topic in an interview.

### What is a Saga?
A **Saga** is a pattern for managing distributed transactions across multiple services where a single ACID transaction is impossible. Instead of a 2-phase commit (which couples services), the Saga uses a sequence of local transactions coordinated by events.

### States
```
Initial → PaymentPending → Confirmed
                        ↘ Cancelled
```

### State Machine Definition
```csharp
public class ShipmentOrderStateMachine : MassTransitStateMachine<ShipmentOrderState>
{
    public State PaymentPending { get; }
    public State Confirmed { get; }
    public State Cancelled { get; }

    public Event<ShipmentCreatedEvent> ShipmentCreated { get; }
    public Event<PaymentCompletedEvent> PaymentCompleted { get; }
    public Event<PaymentFailedEvent> PaymentFailed { get; }
    public Event<ShipmentCancelledByCustomerEvent> ShipmentCancelledByCustomer { get; }
}
```

### Transition Logic

**`Initially` (Initial State):**
When `ShipmentCreatedEvent` arrives:
- Record `ShipmentId`, `CustomerId`, `TrackingNumber`, `Amount` in Saga state.
- Transition to `PaymentPending`.

**`During(PaymentPending):`**
- `PaymentCompleted` → Transition to `Confirmed`.
- `PaymentFailed` → Publish `CancelShipmentCommand` (triggers `CancelShipmentConsumer`) → Transition to `Cancelled`.
- `ShipmentCancelledByCustomer` (before payment) → Transition to `Cancelled` directly.

**`During(Confirmed):`**
- `ShipmentCancelledByCustomer` (post-payment) → Transition to `Cancelled`.

### Correlation Strategy
```csharp
Event(() => ShipmentCreated, x => {
    x.CorrelateById(ctx => ctx.Message.CorrelationId);
    x.SelectId(ctx => ctx.Message.CorrelationId);  // Sets saga instance ID
});
```
Every Saga instance is identified by a `CorrelationId` (Guid). This GUID is generated when the shipment is created and passed through ALL subsequent events. MassTransit uses this to route events to the correct Saga instance.

### Persistence
Saga state is persisted using **EF Core EntityFramework Repository** (not InMemory for production):
```csharp
x.AddSagaStateMachine<ShipmentOrderStateMachine, ShipmentOrderState>()
    .EntityFrameworkRepository(r => {
        r.ConcurrencyMode = ConcurrencyMode.Optimistic;
        r.ExistingDbContext<ShipmentDbContext>();
        r.UseSqlServer();
    });
```

**Why `ConcurrencyMode.Optimistic`?** If two events for the same Saga arrive simultaneously, optimistic concurrency uses a `RowVersion` column (in `ShipmentOrderState`) to detect and retry the second write. Pessimistic locking would block concurrent event processing across the system.

**Why `SetCompletedWhenFinalized()`?** Once a Saga reaches a terminal state (Confirmed or Cancelled), MassTransit automatically removes the row from the DB. This prevents the saga table from growing unboundedly.

### Saga vs. Choreography
**Choreography** (alternative): Each service reacts to events and publishes new events. No central coordinator.  
**Orchestration (Saga)**: A central coordinator (the state machine) drives the workflow.

We chose **orchestration** because:
1. The workflow has clear compensation logic (cancel shipment on payment failure) that is easier to reason about in one place.
2. Debugging is easier — one place to check what state a workflow is in.
3. Choreography with complex rollback logic becomes a "spaghetti of events."

---

## Core Code Deep Dive

### `Core/Services/ShipmentService.cs` — Rate Calculation

```csharp
private static decimal CalculateRate(ShipmentType type, Package package)
{
    var volumetric = (package.LengthCm * package.WidthCm * package.HeightCm) / 5000.0;
    var billable = Math.Max(package.WeightKg, volumetric);
    decimal ratePerKg = type switch {
        ShipmentType.Domestic      => 50m,
        ShipmentType.Express       => 100m,
        ShipmentType.International => 150m,
        ShipmentType.Freight       => 30m,
        _ => 50m
    };
    return (decimal)(billable * (double)ratePerKg) + 50m;
}
```

**Volumetric weight** (LxWxH/5000) is an industry standard for shipping — a light but bulky box costs more to ship than its actual weight suggests. This calculation mirrors real courier service logic.

### `Infrastructure/Repositories/ShipmentRepository.cs` — Complex Paged Query

```csharp
var query = _context.Shipments
    .Include(s => s.SenderAddress)
    .Include(s => s.ReceiverAddress)
    .Include(s => s.Package)
    .AsQueryable();
```

Three `.Include()` calls generate a SQL JOIN across 4 tables in a single query. Without `Include`, EF Core would issue N+1 queries (1 for shipments + 1 for each address + 1 for each package).

The dynamic filter + sort + pagination pattern:
```csharp
query = req.SortBy?.ToLower() switch {
    "status" => req.SortOrder == "asc" ? query.OrderBy(s => s.Status) : query.OrderByDescending(s => s.Status),
    "rate"   => req.SortOrder == "asc" ? query.OrderBy(s => s.ShippingRate) : query.OrderByDescending(s => s.ShippingRate),
    _ => query.OrderByDescending(s => s.CreatedAt) // Default
};
var totalCount = await query.CountAsync(); // Count before pagination!
var items = await query.Skip(...).Take(...).ToListAsync();
```

**Why `CountAsync()` before `.Skip().Take()`?** Pagination UI needs `TotalCount` to calculate total pages. `CountAsync()` runs `SELECT COUNT(*)` with all filters applied — this is the correct total *matching* items, not total in table.

---

## Key Technologies & Libraries Used

| Technology | Why Used |
|---|---|
| **MassTransit + RabbitMQ** | Async event bus; Saga orchestration framework |
| **MassTransit StateMachine** | Distributed saga with EF Core persistence |
| **Entity Framework Core** | ORM with eager loading, migrations, concurrency tokens |
| **FluentValidation** | Business rule validation (rate limits, enum checks, future dates) |
| **Microsoft.AspNetCore.Authentication.JwtBearer** | JWT validation for customer ID extraction |
| **Aspire.HealthChecks.SqlServer/RabbitMQ** | Infrastructure health probes |
| **Serilog** | Structured request logging |

---

## Data Flow Examples

### Flow: Customer Creates Shipment → Payment → Saga Confirms

```
1. POST /gateway/shipments → ShipmentService
   ShipmentCreated DB + ShipmentCreatedEvent published

2. RabbitMQ → Saga (ShipmentCreated)
   Saga state: Initial → PaymentPending (stored in DB)

3. RabbitMQ → PaymentService (ShipmentCreated)
   Stores CorrelationId mapping for later

4. RabbitMQ → NotificationService (ShipmentCreated)
   Sends "Shipment Created" email

5. POST /gateway/payment/create-order → PaymentService
   Creates Razorpay order → returns payment link

6. Client pays via Razorpay → POST /gateway/payment/verify
   PaymentService marks payment as Paid
   Publishes PaymentCompletedEvent

7. RabbitMQ → Saga (PaymentCompleted)
   Saga state: PaymentPending → Confirmed

8. PATCH /gateway/shipments/{id}/schedule-pickup
   ShipmentService checks payment (HTTP call) → Status Draft→Booked
   Publishes ShipmentStatusUpdatedEvent

9. RabbitMQ → TrackingService (StatusUpdated) → creates tracking event
   RabbitMQ → NotificationService (StatusUpdated) → sends status email
```

---

## Interview-Ready Insights

### Potential Interview Questions

1. **"Explain the Saga pattern and why it was used here."**
   → Distributed transaction across Shipment and Payment services. Saga coordinates via events; compensating transaction (cancel shipment) on payment failure. Prevents orphaned draft shipments.

2. **"What is `ConcurrencyMode.Optimistic` in the Saga?"**
   → Uses `RowVersion` byte array column. EF Core includes `WHERE RowVersion = @original` in UPDATE queries. If two concurrent updates race, the second one fails with `DbUpdateConcurrencyException` and is retried. Prevents race conditions in the Saga.

3. **"What is the difference between the shipment status update in Admin vs Customer cancel?"**
   → Admin updates status via REST (operational control). Customer cancellation goes through event flow: ShipmentCancelledByCustomerEvent → Saga handles it + PaymentService may issue refund.

4. **"Why does `SchedulePickupAsync` make an HTTP call to PaymentService?"**
   → It needs real-time payment status before allowing pickup. RabbitMQ events are not real-time queries — they are fire-and-forget. An HTTP call gets an immediate synchronous answer.

5. **"How is row-level security implemented for customers?"**
   → `GetByIdAndCustomerAsync(id, customerId)` — the repository query includes `WHERE Id = @id AND CustomerId = @customerId`. A customer injecting another shipment's ID gets a 404.

### Potential Improvements

- **Event Sourcing:** Store all status transitions as immutable events instead of updating a `Status` column. Provides full audit trail and time-travel queries.
- **Outbox Pattern:** Current `Publish` after `SaveChanges` can lose events. Transactional Outbox would guarantee at-least-once delivery.
- **Saga Compensation Timeout:** If `PaymentCompleted` never arrives (user abandons payment), the Saga stays in `PaymentPending` forever. A timeout (e.g., 24 hours) should trigger `CancelShipmentCommand`.
- **Rate Calculation Service:** Currently hardcoded per shipment type. A dedicated `PricingService` with configurable rate cards would be more flexible.

### Trade-offs Made

| Decision | Trade-off |
|---|---|
| Saga orchestration | Clear workflow vs. saga's inherent complexity |
| HTTP call for payment check on pickup | Real-time answer vs. service coupling |
| Hard deletes on UserDeleted | Simple; soft deletes would preserve historical data |
| Inline rate calculation | Simple; dedicated pricing service would be extensible |
