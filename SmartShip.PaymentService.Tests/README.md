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


