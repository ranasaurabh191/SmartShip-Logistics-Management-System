# SmartShip.PaymentService — Test Suite

## Overview

Full test coverage for the **PaymentService** microservice in the SmartShip Logistics Management System.
Framework: **xUnit** | Assertions: **FluentAssertions** | Mocking: **Moq** | Transport: **MassTransit InMemory**

- **Total Tests:** 37
- **Unit Tests:** 27
- **Integration Tests:** 10
- **Status:** ✅ All Passing

---

## Running the Tests

```bash
cd SmartShip.PaymentService.Tests
dotnet test --verbosity normal
```

Expected output: Test summary: total: 37, failed: 0, succeeded: 37, skipped: 0


---

## Test Infrastructure

### WebApplicationFactory (Integration Tests)
- Uses `UseEnvironment("Testing")` to skip `db.Database.Migrate()` in `Program.cs`
- Replaces **SqlServer** `DbContext` with **EF Core InMemory** provider
- Replaces **RabbitMQ** MassTransit transport with **InMemory** (`loopback://localhost/`)
- Removes raw `RabbitMQ.Client.IConnection` singleton (no broker needed)
- Strips SQL Server + RabbitMQ **health checks** to prevent infra connection on startup
- Each test class gets a **unique InMemory database** (`Guid.NewGuid()` suffix)

### MockHttpClientFactory
- Intercepts all `IHttpClientFactory.CreateClient("ShipmentService")` calls
- Returns pre-baked JSON responses via `MockHttpMessageHandler`
- Variants: `WithResponse(object)`, `WithNotFound()`

### MockPublishEndpoint
- In-memory implementation of `IPublishEndpoint`
- Supports `WasPublished<T>()` and `GetPublished<T>()` assertions
- Call `Reset()` between tests when needed

### MockHttpContext
- `MockHttpContext.WithUserId(int userId)` — injects JWT claims into `IHttpContextAccessor`
- `MockHttpContext.Unauthenticated()` — returns context with no claims

### TestJwtHelper
- Generates signed JWT tokens for integration test authenticated requests

---

## Unit Tests

### `PaymentServiceTests_CreateOrder` (10 tests)

| # | Test Name | Description |
|---|-----------|-------------|
| 1 | `CreateOrderAsync_Online_ReturnsSuccessResponse` | Online order returns correct response with `Pending` status and mock `order_MOCK_` ID |
| 2 | `CreateOrderAsync_Online_PublishesOnlyPaymentCreatedEvent` | Online order publishes `PaymentCreatedEvent` only — `PaymentCompletedEvent` must NOT be published |
| 3 | `CreateOrderAsync_Online_SavesCorrectPaymentEntity` | Verifies saved entity has correct `ShipmentId`, `CustomerId`, `Amount`, `SagaCorrelationId` |
| 4 | `CreateOrderAsync_COD_PublishesBothEvents` | COD order publishes both `PaymentCreatedEvent` and `PaymentCompletedEvent` with `PaymentMethod = "COD"` |
| 5 | `CreateOrderAsync_ShipmentNotFound_ThrowsKeyNotFoundException` | HTTP 404 from ShipmentService throws `KeyNotFoundException` with message containing "Shipment not found" |
| 6 | `CreateOrderAsync_WrongOwner_ThrowsUnauthorizedAccessException` | Shipment owned by different customer throws `UnauthorizedAccessException` |
| 7 | `CreateOrderAsync_AlreadyPaid_ThrowsInvalidOperationException` | Existing `Paid` payment throws `InvalidOperationException` with "already paid" |
| 8 | `CreateOrderAsync_CODAlreadyRegistered_ThrowsInvalidOperationException` | Existing COD payment throws `InvalidOperationException` with "COD" in message |
| 9 | `CreateOrderAsync_OnlineAlreadyInitiated_ThrowsInvalidOperationException` | Existing Online/Pending payment throws `InvalidOperationException` with "already initiated" |
| 10 | `CreateOrderAsync_NoToken_ThrowsUnauthorizedAccessException` | Missing JWT token throws `UnauthorizedAccessException` |

---

### `PaymentServiceTests_VerifyPayment` (9 tests)

| # | Test Name | Description |
|---|-----------|-------------|
| 11 | `VerifyPaymentAsync_ValidRequest_ReturnsSuccess` | Valid verify request marks payment as `Paid` and returns success response |
| 12 | `VerifyPaymentAsync_ValidRequest_PublishesPaymentCompletedEvent` | Publishes `PaymentCompletedEvent` with correct `CorrelationId` and `PaymentStatus = "Paid"` |
| 13 | `VerifyPaymentAsync_ValidRequest_UpdatesPaymentFields` | Verifies `RazorpayPaymentId`, `Signature`, and `PaidAt` are saved correctly |
| 14 | `VerifyPaymentAsync_InvalidOrderId_ThrowsKeyNotFoundException` | Unknown `RazorpayOrderId` throws `KeyNotFoundException` and publishes `PaymentFailedEvent` |
| 15 | `VerifyPaymentAsync_InvalidOrderId_MarksPaymentAsFailed` | Existing payment is marked `Failed` in DB when order ID doesn't match |
| 16 | `VerifyPaymentAsync_InvalidOrderId_PublishesPaymentFailedEvent` | `PaymentFailedEvent` is always published even when `CorrelationId` is `Guid.Empty` |
| 17 | `VerifyPaymentAsync_WrongOwner_ThrowsUnauthorizedAccessException` | Payment owned by different user throws `UnauthorizedAccessException` |
| 18 | `VerifyPaymentAsync_AlreadyPaid_ThrowsInvalidOperationException` | Re-verifying an already `Paid` payment throws `InvalidOperationException` |
| 19 | `VerifyPaymentAsync_NoToken_ThrowsUnauthorizedAccessException` | Missing JWT token throws `UnauthorizedAccessException` |

---

### `PaymentServiceTests_PaymentStatus` (5 tests)

| # | Test Name | Description |
|---|-----------|-------------|
| 20 | `PaymentStatusAsync_ByOrderId_ReturnsCorrectStatus` | Fetches payment by `RazorpayOrderId` and returns correct response |
| 21 | `PaymentStatusAsync_ByShipmentId_ReturnsCorrectStatus` | Fetches payment by `ShipmentId` and returns correct response |
| 22 | `PaymentStatusAsync_ByTrackingNumber_ReturnsCorrectStatus` | Fetches payment by `TrackingNumber` and returns correct response |
| 23 | `PaymentStatusAsync_NotFound_ThrowsKeyNotFoundException` | No matching payment throws `KeyNotFoundException` |
| 24 | `PaymentStatusAsync_NoParamsProvided_ThrowsKeyNotFoundException` | All-null request throws `KeyNotFoundException` |

---

### `PaymentServiceTests_GetByShipment` (3 tests)

| # | Test Name | Description |
|---|-----------|-------------|
| 25 | `GetByShipmentIdAsync_Found_ReturnsResponse` | Returns correct payment response for valid `ShipmentId` |
| 26 | `GetByShipmentIdAsync_NotFound_ThrowsKeyNotFoundException` | Missing shipment payment throws `KeyNotFoundException` |
| 27 | `GetByShipmentIdAsync_CODPending_ReturnsCorrectMessage` | COD pending payment returns "Pay on delivery" message |

---

## Integration Tests

### `PaymentControllerIntegrationTests` (10 tests)

| # | Test Name | Expected Status | Description |
|---|-----------|----------------|-------------|
| 28 | `POST_CreateOrder_WithoutToken_Returns401` | 401 | `POST /api/payment/create-order` rejected without JWT |
| 29 | `POST_Verify_WithoutToken_Returns401` | 401 | `POST /api/payment/verify` rejected without JWT |
| 30 | `GET_ShipmentPayment_WithoutToken_Returns401` | 401 | `GET /api/payment/shipment/{id}` rejected without JWT |
| 31 | `GET_PaymentStatus_WithoutToken_Returns401` | 401 | `GET /api/payment/payment-status` rejected without JWT |
| 32 | `GET_ShipmentPayment_NotFound_Returns404` | 404 | Non-existent `ShipmentId=99999` returns 404 |
| 33 | `GET_PaymentStatus_ByOrderId_NotFound_Returns404` | 404 | Non-existent `razorpayOrderId` returns 404 |
| 34 | `GET_PaymentStatus_ByShipmentId_NotFound_Returns404` | 404 | Non-existent `shipmentId=99999` returns 404 |
| 35 | `POST_CreateOrder_RouteExists_DoesNotReturn404` | ≠ 404 | Route `/api/payment/create-order` is registered and reachable |
| 36 | `GET_PaymentStatus_WithToken_RouteExists` | ≠ 404 | Route `/api/payment/payment-status` is registered and reachable |
| 37 | `GET_ShipmentPayment_WithToken_RouteExists` | ≠ 404 | Route `/api/payment/shipment/{id}` is registered and reachable |

---

## Exception → HTTP Status Code Mapping

| Exception | HTTP Status |
|-----------|-------------|
| `KeyNotFoundException` | 404 Not Found |
| `UnauthorizedAccessException` | 401 Unauthorized |
| `ArgumentException` | 400 Bad Request |
| `InvalidOperationException` | 409 Conflict |
| `NotImplementedException` | 501 Not Implemented |
| `TimeoutException` | 408 Request Timeout |
| Unhandled | 500 Internal Server Error |

---

## Project Structure
SmartShip.PaymentService.Tests/
├── UnitTests/
│ └── Services/
│ ├── PaymentServiceTests_CreateOrder.cs
│ ├── PaymentServiceTests_VerifyPayment.cs
│ ├── PaymentServiceTests_PaymentStatus.cs
│ └── PaymentServiceTests_GetByShipment.cs
├── IntegrationTests/
│ └── Controllers/
│ └── PaymentControllerIntegrationTests.cs
└── Helpers/
├── MockHttpClientFactory.cs
├── MockHttpMessageHandler.cs
├── MockPublishEndpoint.cs
├── MockHttpContext.cs
└── TestJwtHelper.cs



SmartShip.PaymentService — Unit Test Suite
Overview
The SmartShip.PaymentService.Tests project is a pure unit test library for the PaymentService business logic layer. It uses xUnit, Moq, and FluentAssertions to test the PaymentService class in complete isolation — no database, no RabbitMQ, no HTTP calls. All external dependencies are mocked via purpose-built helper classes. This suite covers three service operations split into three focused test classes, with 30+ individual test cases covering happy paths, edge cases, authorization failures, and event publishing behavior.

Project Structure
text
SmartShip.PaymentService.Tests/
├── Helpers/
│   ├── MockHttpContext.cs          # Fakes IHttpContextAccessor with JWT claims
│   ├── MockHttpClientFactory.cs    # Fakes IHttpClientFactory (success/404 responses)
│   └── TestJwtHelper.cs            # Generates real signed JWT tokens for integration use
├── Mocks/
│   └── MockPublishEndpoint.cs      # In-memory IPublishEndpoint — captures published events
└── UnitTests/
    └── Services/
        ├── PaymentServiceTests_CreateOrder.cs
        ├── PaymentServiceTests_VerifyPayment.cs
        └── PaymentServiceTests_GetStatus.cs
Test Infrastructure — Helpers & Mocks
MockHttpContext.cs
Fakes IHttpContextAccessor — the mechanism by which PaymentService reads the authenticated user's userId claim from the JWT.

csharp
MockHttpContext.WithUserId(29)       // authenticated user, userId claim = 29
MockHttpContext.Unauthenticated()    // no claims — triggers UnauthorizedAccessException
Why needed: PaymentService extracts userId from HttpContext.User claims on every operation. Without this mock, tests would require a full ASP.NET pipeline.

MockHttpClientFactory.cs
Fakes IHttpClientFactory — used by PaymentService to call ShipmentService's GET /api/shipments/{id} endpoint during order creation.

csharp
MockHttpClientFactory.WithResponse(shipmentDto)   // returns 200 OK with serialized DTO
MockHttpClientFactory.WithNotFound()              // returns 404 — triggers KeyNotFoundException
Why needed: CreateOrderAsync calls ShipmentService over HTTP to validate the shipment exists and verify ownership. This mock eliminates that network dependency entirely.

MockPublishEndpoint.cs
A full in-memory implementation of MassTransit's IPublishEndpoint. Captures all published messages in a typed list for assertion.

csharp
_publisher.WasPublished<PaymentCompletedEvent>()   // bool — was this event published?
_publisher.GetPublished<PaymentCompletedEvent>()   // T? — get the published instance
_publisher.Reset()                                 // clear between tests
_publisher.PublishedMessages                       // full list of all published events
Why a real implementation, not a Mock<IPublishEndpoint>?
MassTransit's IPublishEndpoint has 10+ overloads of Publish. Using Mock<T> would require setting up each overload individually. A concrete implementation captures all published messages regardless of which overload is called.

TestJwtHelper.cs
Generates real HMAC-SHA256 signed JWT tokens for integration test scenarios (not used in unit tests — unit tests use MockHttpContext directly).

csharp
TestJwtHelper.GenerateToken(userId: 29, role: "Customer")
Claim	Value
userId	provided int
ClaimTypes.NameIdentifier	same int
ClaimTypes.Role	provided role string
Test Classes
PaymentServiceTests_CreateOrder — 9 tests
Tests CreateOrderAsync — the entry point for both COD and Online payment order creation. Requires ShipmentService HTTP validation and saga correlation.

Test	What it verifies
Online_ReturnsSuccessResponse	Returns Pending status + mock Razorpay order ID
Online_PublishesOnlyPaymentCreatedEvent	Online does NOT publish PaymentCompletedEvent
Online_SavesCorrectPaymentEntity	Correct entity fields saved to repo (amount, customerId, sagaId)
COD_PublishesBothEvents	COD publishes both PaymentCreatedEvent + PaymentCompletedEvent
ShipmentNotFound_ThrowsKeyNotFoundException	404 from ShipmentService → exception
WrongOwner_ThrowsUnauthorizedAccessException	Shipment belongs to different customer
AlreadyPaid_ThrowsInvalidOperationException	Duplicate payment attempt on paid shipment
CODAlreadyRegistered_ThrowsInvalidOperationException	COD already registered → blocks retry
OnlineAlreadyInitiated_ThrowsInvalidOperationException	Online payment already pending
NoToken_ThrowsUnauthorizedAccessException	Missing/empty JWT claims
Key design — COD vs Online split:
COD publishes PaymentCompletedEvent immediately (no user action needed to "complete" payment). Online only publishes PaymentCreatedEvent — PaymentCompletedEvent comes later via VerifyPaymentAsync. Tests explicitly assert this asymmetry.

PaymentServiceTests_VerifyPayment — 10 tests
Tests VerifyPaymentAsync — the Razorpay callback handler that marks a payment as Paid and advances the Saga.

Test	What it verifies
ValidRequest_ReturnsSuccessResponse	Returns "Paid" status + success message
ValidRequest_MarksPaymentAsPaid	Mutates entity: status=Paid, paymentId, signature, PaidAt
ValidRequest_PublishesPaymentCompletedEvent	Correct CorrelationId, ShipmentId, PaymentMethod in event
InvalidOrderId_ThrowsKeyNotFoundException	Unknown order ID → exception
InvalidOrderId_MarksPaymentAsFailed	Payment entity updated to Failed before exception throws
InvalidOrderId_PublishesPaymentFailedEvent	PaymentFailedEvent published with correct Reason
InvalidOrderId_StillPublishesEvent_WhenNoSagaCorrelation	Publishes with CorrelationId = Guid.Empty if saga missing
WrongOwner_ThrowsUnauthorizedAccessException	Token userId ≠ payment.CustomerId
AlreadyPaid_ThrowsInvalidOperationException	Duplicate verify attempt blocked
NoToken_ThrowsUnauthorizedAccessException	No JWT claims
Critical path — failure sequence:
When an invalid order ID is submitted, the service does three things before throwing: (1) marks existing payment as Failed, (2) saves to DB, (3) publishes PaymentFailedEvent. Tests _MarksPaymentAsFailed, _PublishesPaymentFailedEvent, and _StillPublishesEvent_WhenNoSagaCorrelation verify each of these independently.

PaymentServiceTests_GetStatus — 8 tests
Tests PaymentStatusAsync and GetByShipmentIdAsync — read-only query methods with no auth check, no HTTP calls, and no event publishing.

Test	What it verifies
GetByShipmentIdAsync_Found_ReturnsMappedResponse	Full response mapping including Message
GetByShipmentIdAsync_PendingOnline_ReturnsInitiatedMessage	Correct message for pending online
GetByShipmentIdAsync_PendingCOD_ReturnsCODMessage	Correct message for COD
GetByShipmentIdAsync_Failed_ReturnsFailedMessage	Correct message for failed payment
GetByShipmentIdAsync_NotFound_ThrowsKeyNotFoundException	Missing record → exception
PaymentStatusAsync_ByOrderId_ReturnsCorrectPayment	Lookup by Razorpay order ID
PaymentStatusAsync_ByShipmentId_ReturnsCorrectPayment	Lookup by shipment ID
PaymentStatusAsync_ByTrackingNumber_ReturnsCorrectPayment	Lookup by tracking number
PaymentStatusAsync_NotFound_ThrowsKeyNotFoundException	All three lookups fail → exception
Lookup priority in PaymentStatusAsync: OrderId → ShipmentId → TrackingNumber. If all three are null, payment stays null → KeyNotFoundException. Tests verify each lookup independently.

Event Publishing Matrix
Method	COD	Online
CreateOrderAsync	PaymentCreatedEvent ✅ PaymentCompletedEvent ✅	PaymentCreatedEvent ✅ PaymentCompletedEvent ❌
VerifyPaymentAsync (success)	N/A	PaymentCompletedEvent ✅
VerifyPaymentAsync (failure)	N/A	PaymentFailedEvent ✅
Technologies Used
Library	Purpose
Library	Purpose
xUnit	Test framework — [Fact] based test discovery
Moq	Mocking IPaymentRepository, ISagaCorrelationRepository, IUnitOfWork
FluentAssertions	Readable assertions — .Should().Be(), .ThrowAsync<T>()
Microsoft.Extensions.Logging.Abstractions	NullLogger<T> — discards all log output in tests
Interview-Ready Insights
"Why split tests into three files?"
Each file maps to one public method group with a distinct setup profile. CreateOrder needs MockHttpClientFactory; VerifyPayment needs saga + payment repo; GetStatus is read-only. Mixing them would bloat setup methods and make failures harder to diagnose.

"Why MockPublishEndpoint instead of Mock<IPublishEndpoint>?"
MassTransit's interface has 10+ Publish overloads. A concrete implementation captures all messages generically without per-overload setup — and supports typed retrieval via GetPublished<T>().

"Why does BuildService() recreate the service per test?"
MockPublishEndpoint retains state across calls. Recreating BuildService() per test (and calling _publisher.Reset() where needed) ensures no cross-test message contamination.

"What's not tested here?"
Saga state machine transitions, RabbitMQ consumer behavior, and HTTP integration — those belong in integration tests using MassTransit's InMemoryTestHarness and WebApplicationFactory.