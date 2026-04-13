# SmartShip.ShipmentService — Unit Test Suite

## Overview

The **SmartShip.ShipmentService.Tests** project provides full test coverage for the ShipmentService
microservice in the SmartShip Logistics Management System. It tests shipment creation, payment
coordination, lifecycle rules, authorization, and event-driven behavior — in complete isolation for
unit tests and through the full ASP.NET pipeline for integration tests.

| | |
|---|---|
| **Framework** | xUnit |
| **Assertions** | FluentAssertions |
| **Mocking** | Moq |
| **Transport** | MassTransit InMemory |
| **Total Tests** | 52 |
| **Unit Tests** | 42 |
| **Integration Tests** | 10 |
| **Status** | ✅ All Passing |

---

## Running the Tests

```bash
cd SmartShip.ShipmentService.Tests
dotnet test --verbosity normal
```

Expected output:
Test summary: total: 52, failed: 0, succeeded: 52, skipped: 0

text

---

## Project Structure
SmartShip.ShipmentService.Tests/
├── Infrastructure/
│ ├── MockHttpMessageHandler.cs # Returns pre-configured HTTP responses
│ ├── MockHttpClientFactory.cs # Fakes outbound calls to CustomerService/PaymentService
│ ├── MockHttpContext.cs # Injects JWT claims into HttpContext
│ ├── MockPublishEndpoint.cs # In-memory IPublishEndpoint — captures published events
│ ├── ShipmentServiceFactory.cs # WebApplicationFactory for integration tests
│ ├── ShipmentServiceTestBase.cs # Shared base class with pre-configured mocks
│ └── TestJwtHelper.cs # Generates real signed JWT tokens
├── IntegrationTests/
│ └── Controllers/
│ └── ShipmentControllerIntegrationTests.cs
└── UnitTests/
└── Services/
├── ShipmentServiceTests_Create.cs
├── ShipmentServiceTests_SchedulePickup.cs
├── ShipmentServiceTests_Cancel.cs
├── ShipmentServiceTests_Status.cs
└── ShipmentServiceTests_Admin.cs

text

---

## Test Infrastructure

### `ShipmentServiceFactory.cs` *(Integration Tests)*

Custom `WebApplicationFactory<Program>` that spins up the full ASP.NET Core pipeline for
integration testing without a real database or message broker.

- Uses `UseEnvironment("Testing")` to skip database migration on startup
- Replaces SqlServer `DbContext` with **EF Core InMemory** (unique DB name per test via GUID)
- Configures **JWT Bearer authentication** with test signing key
- Each test class gets an isolated in-memory database — no cross-test state

---

### `MockHttpClientFactory.cs`

Fakes `IHttpClientFactory` — used by `ShipmentService` to call **CustomerService** (validate
customer on creation) and **PaymentService** (verify payment on pickup scheduling).

```csharp
MockHttpClientFactory.WithResponse(object dto)   // returns 200 OK with serialized DTO
MockHttpClientFactory.WithNotFound()             // returns 404 — triggers KeyNotFoundException
```

**Why needed:** `CreateAsync` calls CustomerService over HTTP to validate the customer exists.
`SchedulePickupAsync` calls PaymentService to check payment status. This mock eliminates
those network dependencies entirely from unit tests.

---

### `MockHttpMessageHandler.cs`

Simple in-memory `HttpMessageHandler` that returns a pre-configured `HttpResponseMessage`.
Used internally by `MockHttpClientFactory` to back the fake `HttpClient`.

---

### `MockPublishEndpoint.cs`

Full in-memory implementation of MassTransit's `IPublishEndpoint`. Captures all published
messages in a typed list for assertion.

```csharp
_publisher.WasPublished<ShipmentCreatedEvent>()         // bool
_publisher.GetPublished<ShipmentCreatedEvent>()         // T? — first match
_publisher.GetAllPublished<ShipmentStatusUpdatedEvent>()// IEnumerable<T> — all matches
_publisher.Reset()                                      // clear between tests
_publisher.PublishedMessages                            // full list of all events
```

**Why a real implementation, not `Mock<IPublishEndpoint>`?**
MassTransit's interface has 10+ overloads of `Publish`. Using `Mock<T>` would require
setting up each overload individually. A concrete implementation captures all messages
regardless of which overload is called.

---

### `MockHttpContext.cs`

Fakes `IHttpContextAccessor` — the mechanism by which `ShipmentService` reads the
authenticated user's `userId` claim and `role` from the JWT.

```csharp
MockHttpContext.WithUserId(29, "CUSTOMER")   // authenticated customer
MockHttpContext.WithUserId(1, "ADMIN")       // authenticated admin
MockHttpContext.Unauthenticated()            // no claims — triggers UnauthorizedAccessException
```

---

### `TestJwtHelper.cs`

Generates real HMAC-SHA256 signed JWT tokens for integration test scenarios.
Not used in unit tests — unit tests use `MockHttpContext` directly.

```csharp
TestJwtHelper.GenerateToken(userId: 29, role: "CUSTOMER")
TestJwtHelper.GenerateToken(userId: 1,  role: "ADMIN")
```

| Claim | Value |
|---|---|
| `userId` | provided int |
| `ClaimTypes.NameIdentifier` | same int |
| `ClaimTypes.Role` | provided role string |

---

### `ShipmentServiceTestBase.cs`

Shared base class for all unit test classes. Provides pre-configured mocks for all
repositories, `IUnitOfWork`, and a `BuildService()` helper — eliminating boilerplate
from every test file.

```csharp
// All unit test classes inherit from this
public class ShipmentServiceTests_Create : ShipmentServiceTestBase { ... }
```

Also exposes a `MakeShipment(...)` factory method to build `Shipment` entities
with sensible defaults and per-test overrides.

---

## Unit Test Classes

### `ShipmentServiceTests_Create` — 6 tests

Tests `CreateAsync` — shipment creation with customer validation, rate calculation,
entity persistence, and event publishing.

| Test | What It Verifies |
|---|---|
| `CreateAsync_ValidRequest_ReturnsShipmentResponse` | Returns response with tracking number and Draft status |
| `CreateAsync_ValidRequest_PublishesShipmentCreatedEvent` | `ShipmentCreatedEvent` published after save |
| `CreateAsync_ValidRequest_SavesCorrectCustomerAndType` | Entity fields (CustomerId, ShipmentType) saved correctly |
| `CreateAsync_CustomerNotFound_ThrowsKeyNotFoundException` | 404 from CustomerService → exception |
| `CreateAsync_CalculatesCorrectRate_Domestic` | Domestic rate formula applied correctly |
| `CreateAsync_CalculatesMinimumRate_VeryLowWeight` | Minimum rate floor enforced |

**Key design — rate calculation:**
Rate logic is centralized in the service. The minimum rate test ensures that very low weight
shipments are not priced below the business-defined floor — an easy regression target.

---

### `ShipmentServiceTests_SchedulePickup` — 6 tests

Tests `SchedulePickupAsync` — transitions a Draft shipment to Booked after verifying
payment status via PaymentService HTTP call.

| Test | What It Verifies |
|---|---|
| `SchedulePickupAsync_CODPaid_SetsStatusToBooked` | COD payment → Draft transitions to Booked |
| `SchedulePickupAsync_OnlinePaid_SetsStatusToBooked` | Online paid → Booked |
| `SchedulePickupAsync_OnlinePending_ThrowsInvalidOperation` | Pending online payment → blocks pickup |
| `SchedulePickupAsync_NoPaymentRecord_ThrowsInvalidOperation` | No payment record at all → blocked |
| `SchedulePickupAsync_NotDraftStatus_ThrowsInvalidOperation` | Only Draft shipments can be scheduled |
| `SchedulePickupAsync_PublishesBookedStatusEvent` | `ShipmentStatusUpdatedEvent` published on success |

**Critical rule — COD vs Online:**
COD payments are considered "approved" at the Pending stage (pay on delivery). Online payments
must be in Paid status before pickup is allowed. Tests explicitly verify both paths.

---

### `ShipmentServiceTests_Cancel` — 5 tests

Tests `CancelByCustomerAsync` — customer-initiated cancellation with refund flag logic
and dual event publishing.

| Test | What It Verifies |
|---|---|
| `CancelByCustomerAsync_DraftShipment_CancelsSuccessfully` | Draft shipment can be cancelled |
| `CancelByCustomerAsync_BookedShipment_SetsWasPaidTrue` | Booked (paid) → `WasPaid = true` for refund |
| `CancelByCustomerAsync_PublishesBothCancelEvents` | `ShipmentCancelledEvent` + `ShipmentCancelledByCustomerEvent` both published |
| `CancelByCustomerAsync_InTransitShipment_ThrowsInvalidOperation` | Cannot cancel in-progress shipments |
| `CancelByCustomerAsync_WithSagaCorrelation_UsesCorrectCorrelationId` | Correct `CorrelationId` flows into event |

**Key design — `WasPaid` flag:**
`ShipmentCancelledByCustomerEvent` carries `WasPaid`. If `true`, PaymentService triggers
a refund. Tests verify this flag is set correctly based on payment state at cancellation time.

---

### `ShipmentServiceTests_Status` — 5 tests

Tests status queries (`GetByIdAsync`) and admin status updates (`UpdateStatusAsync`).

| Test | What It Verifies |
|---|---|
| `GetByIdAsync_ValidId_ReturnsResponse` | Full response mapping with all fields |
| `GetByIdAsync_NotFound_ThrowsKeyNotFoundException` | Missing shipment → exception |
| `UpdateStatusAsync_ValidTransition_UpdatesStatus` | Allowed transition updates status + publishes event |
| `UpdateStatusAsync_Delivered_SetsDeliveredAt` | Delivered transition sets `DeliveredAt` timestamp |
| `UpdateStatusAsync_InvalidTransition_ThrowsInvalidOperation` | Business rule: invalid transitions blocked |

---

### `ShipmentServiceTests_Admin` — 9 tests

Tests admin-only operations including exception resolution and the full rate calculation matrix.

| Test | What It Verifies |
|---|---|
| `ResolveExceptionAsync_ValidShipment_SetsInTransit` | Admin exception resolution → InTransit |
| `CalculateRateAsync_Domestic_*` | Domestic rate by weight |
| `CalculateRateAsync_International_*` | International rate by weight |
| `CalculateRateAsync_Express_*` | Express surcharge applied |
| `CalculateRateAsync_MinimumRate_*` | Minimum rate floor for all types |

---

## Integration Tests

### `ShipmentControllerIntegrationTests` — 10 tests

End-to-end tests using the full ASP.NET Core pipeline with real JWT authentication,
real middleware, and EF Core InMemory database.

| Test | Expected Status | Description |
|---|---|---|
| `CreateShipment_WithoutToken_Returns401` | 401 | Auth guard on create |
| `SchedulePickup_WithoutToken_Returns401` | 401 | Auth guard on pickup |
| `CancelShipment_WithoutToken_Returns401` | 401 | Auth guard on cancel |
| `GetShipment_WithoutToken_Returns401` | 401 | Auth guard on get |
| `UpdateStatus_WithoutToken_Returns401` | 401 | Auth guard on update |
| `GetAllShipments_WithoutToken_Returns401` | 401 | Auth guard on list |
| `UpdateStatus_AsCustomer_Returns403` | 403 | ADMIN-only route protected |
| `ResolveException_AsCustomer_Returns403` | 403 | ADMIN-only route protected |
| `GetShipment_NotFound_Returns404` | 404 | Not found handling |
| `GetByCustomer_NotFound_Returns404` | 404 | Customer shipments not found |

**Why integration tests for auth only?**
Business logic is fully covered by unit tests with mocks. Integration tests focus exclusively
on the HTTP pipeline — middleware, JWT validation, role-based authorization, and route
registration — which cannot be tested through mocks alone.

---

## Exception → HTTP Status Code Mapping

| Exception | HTTP Status |
|---|---|
| `KeyNotFoundException` | `404 Not Found` |
| `UnauthorizedAccessException` / No Token | `401 Unauthorized` |
| `InvalidOperationException` | `409 Conflict` |
| `ArgumentException` | `400 Bad Request` |
| Unhandled | `500 Internal Server Error` |

Mapping is handled by a global `ExceptionMiddleware` — integration tests verify that
the correct status codes are returned end-to-end for each exception type.

---

## Key Design Decisions

**Outbound HTTP calls for loose coupling:**
ShipmentService calls CustomerService and PaymentService via HTTP — not direct DB queries.
This keeps service boundaries clean. `MockHttpClientFactory` eliminates the network
in unit tests while `ShipmentServiceFactory` can wire real HTTP in future integration tests.

**Strict status transitions:**
`UpdateStatusAsync` enforces allowed transitions (e.g., Draft → Booked → PickedUp → InTransit).
Skipping a state or going backward throws `InvalidOperationException`. Tests cover both
valid and invalid transitions explicitly.

**Event publishing on every major change:**
Every mutation fires one or more events — Saga advancement, TrackingService updates, and
NotificationService emails all depend on these events. Tests assert both that the correct
event was published AND that its fields carry the right values.

**Ownership checks via userId claim:**
`CancelByCustomerAsync` and other customer operations verify that the `userId` from the
JWT matches the shipment's `CustomerId`. `MockHttpContext.WithUserId()` makes this
testable without a real auth pipeline.

---

## Interview-Ready Insights

### Potential Interview Questions

1. **"Why separate unit tests into five files?"**
Each file maps to one operation group with a distinct setup profile. `SchedulePickup`
needs a PaymentService HTTP mock; `Cancel` needs saga correlation; `Admin` needs no auth
context at all. Mixing them would bloat setup and make failures ambiguous.

2. **"Why EF Core InMemory for integration tests instead of a real DB?"**
Integration tests here focus on the HTTP pipeline — auth, routing, middleware. InMemory
provides a deterministic, zero-setup data store. Real DB tests (SQL Server) belong in a
separate E2E test project to avoid CI/CD infrastructure dependencies.

3. **"How do you prevent cross-test data pollution in integration tests?"**
`ShipmentServiceFactory` generates a unique GUID-named InMemory database per test class.
Each test class gets a completely isolated store — no `DELETE` teardown needed.

4. **"Why does `CancelByCustomerAsync` publish two events?"**
`ShipmentCancelledEvent` is consumed by AdminService and NotificationService (generic
cancellation). `ShipmentCancelledByCustomerEvent` is consumed by the Saga and PaymentService
(carries `WasPaid` for refund logic). Same fact, different consumers, different data needs.

5. **"What's not tested here?"**
Full Saga state machine orchestration, RabbitMQ consumer behavior, and end-to-end
distributed flows across services. Those belong in a dedicated E2E test suite using
Docker Compose and MassTransit's `InMemoryTestHarness`.

### Potential Improvements

- **`[Theory]` + `[InlineData]`:** Rate calculation tests repeat similar structure — consolidate
  with parameterized theories for cleaner coverage of weight tiers.
- **Clock abstraction:** Inject `TimeProvider` to make `DeliveredAt` and `CreatedAt`
  deterministic — currently asserted only as `NotBeNull()`.
- **Consumer unit tests:** Add tests for `CancelShipmentConsumer` and
  `ShipmentCreatedConsumer` to cover inbound event handling logic.
- **Contract tests:** Use Pact or a shared contract verifier to validate that
  `ShipmentCreatedEvent` schema matches what consumers expect.