# SmartShip.PaymentService.Tests

## Overview

This project is the dedicated test suite for `SmartShip.PaymentService` — the microservice responsible for creating Razorpay payment orders, verifying online payments, handling COD registration, and querying payment status. The test suite is split into two layers: **unit tests** that isolate and verify the core business logic inside `PaymentService.cs` with full mock control, and **integration tests** that spin up the real ASP.NET Core pipeline in-memory to verify HTTP routing, authentication enforcement, and middleware behavior. Together they give confidence that the payment domain behaves correctly in isolation and as a deployed HTTP service.

---

## Overall Architecture & Design Decisions

### Test Architecture Pattern: Dual-Layer (Unit + Integration)

The suite deliberately separates concerns into two distinct test categories:

- **Unit Tests** (`UnitTests/Services/`) — test the `PaymentService` class directly, bypassing HTTP. All dependencies (`IPaymentRepository`, `ISagaCorrelationRepository`, `IUnitOfWork`, `IPublishEndpoint`, `IHttpClientFactory`, `IHttpContextAccessor`) are mocked or faked. This makes tests fast, deterministic, and pinpointed to a single class.
- **Integration Tests** (`IntegrationTests/Controllers/`) — use `WebApplicationFactory<Program>` to boot the full ASP.NET Core pipeline with an in-memory EF Core database and in-memory MassTransit. These tests verify that routes exist, JWT middleware rejects unauthenticated requests, and the `ExceptionMiddleware` maps exceptions to correct HTTP status codes.

### Why this split?

| Concern | Unit Test | Integration Test |
|---|---|---|
| Business logic correctness | ✅ | ❌ |
| HTTP routing | ❌ | ✅ |
| JWT auth enforcement | ❌ | ✅ |
| Event publishing | ✅ | ❌ |
| DB interaction | Mocked | In-memory EF |
| Speed | Very fast | Moderate |

This is the standard **Testing Pyramid** approach: many fast unit tests at the base, fewer slower integration tests at the top.

### How this fits into the bigger system

`PaymentService` is a critical node in the SmartShip saga choreography. It receives `ShipmentCreatedEvent` from RabbitMQ, creates payment records, and publishes `PaymentCreatedEvent`, `PaymentCompletedEvent`, and `PaymentFailedEvent` back to the bus. The tests validate that these events are published with the correct payloads and correlation IDs — which is essential for the distributed saga to advance correctly.

---

## Folder Structure

```
SmartShip.PaymentService.Tests/
├── Helpers/                          # Reusable test infrastructure utilities
│   ├── MockHttpClientFactory.cs      # Builds fake IHttpClientFactory returning controlled responses
│   ├── MockHttpContext.cs            # Builds fake IHttpContextAccessor with injected JWT claims
│   └── TestJwtHelper.cs             # Generates real signed JWTs for integration test auth
├── Mocks/
│   └── MockPublishEndpoint.cs        # In-memory IPublishEndpoint that captures published messages
├── UnitTests/
│   └── Services/
│       ├── PaymentServiceTests_CreateOrder.cs   # Tests for CreateOrderAsync (9 tests)
│       ├── PaymentServiceTests_GetStatus.cs     # Tests for GetByShipmentIdAsync + PaymentStatusAsync (9 tests)
│       └── PaymentServiceTests_VerifyPayment.cs # Tests for VerifyPaymentAsync (10 tests)
├── IntegrationTests/
│   └── Controllers/
│       └── PaymentControllerIntegrationTests.cs # HTTP-level tests (9 tests)
└── SmartShip.PaymentService.Tests.csproj
```

**Why this structure?**
- `Helpers/` and `Mocks/` are shared across all test classes — no duplication.
- Unit tests are split by method group (`_CreateOrder`, `_GetStatus`, `_VerifyPayment`) rather than one giant file. This keeps each file focused and makes it easy to find tests for a specific feature.
- Integration tests are organized by controller, mirroring the production `API/Controllers/` structure.

---

## Test Infrastructure: File-by-File Deep Dive

### `Helpers/MockHttpClientFactory.cs`

**What it does:** Creates a fake `IHttpClientFactory` that returns an `HttpClient` backed by a `Mock<HttpMessageHandler>`. The handler intercepts all outgoing HTTP calls and returns a pre-configured response without hitting any real network.

**Why it was written this way:**

`PaymentService` makes an internal HTTP call to `ShipmentService` (`GET api/shipments/{id}`) to validate shipment ownership before creating a payment. In unit tests, you cannot have a real ShipmentService running. The solution is to mock `HttpMessageHandler` — the lowest-level abstraction in the `HttpClient` stack — using Moq's `Protected()` API, which allows mocking of `protected virtual` methods like `SendAsync`.

```csharp
// Key pattern: mock the protected SendAsync method
handlerMock.Protected()
    .Setup<Task<HttpResponseMessage>>("SendAsync",
        ItExpr.IsAny<HttpRequestMessage>(),
        ItExpr.IsAny<CancellationToken>())
    .ReturnsAsync(response);
```

**Two factory methods:**
- `WithResponse<T>(T responseObject)` — serializes any object to JSON and returns it as a 200 OK. Used to simulate a successful shipment lookup.
- `WithNotFound()` — returns a 404. Used to simulate a missing shipment, triggering `KeyNotFoundException` in the service.

**Trade-off:** This approach mocks at the `HttpMessageHandler` level rather than using a real HTTP test server. It's simpler and faster but doesn't test serialization edge cases. An alternative would be `WireMock.Net` for more realistic HTTP stubbing.

---

### `Helpers/MockHttpContext.cs`

**What it does:** Creates a fake `IHttpContextAccessor` with a `ClaimsPrincipal` that contains a `userId` claim and a `NameIdentifier` claim. This simulates a logged-in user without needing a real JWT token.

**Why it was written this way:**

`PaymentService.CreateOrderAsync` and `VerifyPaymentAsync` extract the authenticated user's ID directly from `IHttpContextAccessor.HttpContext.User.FindFirst("userId")`. In unit tests, there is no real HTTP pipeline, so `IHttpContextAccessor` would return `null` by default. This helper injects a controlled identity.

```csharp
// Two claims are set — matching the dual-claim pattern in the real service
new Claim("userId", userId.ToString()),
new Claim(ClaimTypes.NameIdentifier, userId.ToString())
```

**Why two claims?** The service checks `"userId"` first, then falls back to `ClaimTypes.NameIdentifier`. Both are set to ensure the mock works regardless of which claim the service reads.

**`Unauthenticated()` method:** Returns a `DefaultHttpContext` with no user claims. Used to test the "no token" guard at the top of service methods.

---

### `Helpers/TestJwtHelper.cs`

**What it does:** Generates a real, cryptographically signed JWT token using the same secret key, issuer, and audience as the production service.

**Why it was written this way:**

Integration tests use `WebApplicationFactory<Program>` which boots the real JWT middleware. That middleware validates the token signature, issuer, and audience. A fake or unsigned token would be rejected with 401. The helper generates a token that passes real validation.

```csharp
private const string Secret = "SmartShip$SuperSecret$Key$2026!@#XYZ";
// Must match appsettings.json JwtSettings:Key exactly
```

**Key design decision:** The secret is hardcoded as a constant in the test helper. This is intentional for tests — it must match the test environment's `appsettings.json`. In production, this key is never hardcoded.

**`GenerateToken(int userId, string role)`:** Accepts a role parameter so integration tests can generate both `Customer` and `Admin` tokens to test role-based authorization.

---

### `Mocks/MockPublishEndpoint.cs`

**What it does:** A hand-written fake implementation of MassTransit's `IPublishEndpoint` interface. Instead of sending messages to RabbitMQ, it stores them in an in-memory `List<object>`.

**Why a hand-written fake instead of Moq?**

`IPublishEndpoint` has 10+ overloads of `Publish`. Mocking all of them with Moq would require 10+ `.Setup()` calls. A hand-written fake is cleaner and provides richer assertion helpers:

```csharp
public bool WasPublished<T>() => _messages.OfType<T>().Any();
public T? GetPublished<T>() => _messages.OfType<T>().FirstOrDefault();
public void Reset() => _messages.Clear();
```

**Why this matters for the payment domain:** The saga choreography depends entirely on the correct events being published. `WasPublished<PaymentCompletedEvent>()` and `GetPublished<PaymentCompletedEvent>()` let tests assert not just that an event was published, but that its payload (CorrelationId, ShipmentId, PaymentMethod) is correct.

---

## Unit Tests: Deep Dive

### `PaymentServiceTests_CreateOrder.cs` — 9 Tests

**Setup pattern:** Each test class declares mocks as fields and uses private helper methods (`SetupSaga()`, `SetupNoExistingPayment()`, `SetupSave()`) to avoid repetition. The `BuildService()` method wires all mocks into a real `PaymentService` instance.

```csharp
private PaymentSvc BuildService(IHttpClientFactory httpClientFactory) =>
    new(_paymentRepo.Object, _sagaRepo.Object, _uow.Object, _publisher,
        NullLogger<PaymentSvc>.Instance,
        httpClientFactory,
        MockHttpContext.WithUserId(UserId));
```

**Why `NullLogger`?** Logging is infrastructure, not business logic. `NullLogger` discards all log calls silently, keeping tests focused on behavior.

#### Test: `CreateOrderAsync_Online_ReturnsSuccessResponse`
Verifies the happy path for online payment. Asserts that the returned `PaymentResponse` has `PaymentMethod = "Online"`, `PaymentStatus = "Pending"`, and a `RazorpayOrderId` starting with `"order_MOCK_"` (the mock Razorpay implementation).

#### Test: `CreateOrderAsync_Online_PublishesOnlyPaymentCreatedEvent`
Verifies event isolation: for Online payments, only `PaymentCreatedEvent` is published at order creation time. `PaymentCompletedEvent` must NOT be published — that only happens after `VerifyPaymentAsync` succeeds.

#### Test: `CreateOrderAsync_Online_SavesCorrectPaymentEntity`
Uses a Moq `Callback` to capture the `ShipmentPayment` entity passed to `AddAsync`. Asserts that `ShipmentId`, `CustomerId`, `Amount`, `PaymentStatus`, and `SagaCorrelationId` are all set correctly. This is the most granular test — it verifies the internal state of the entity, not just the response DTO.

#### Test: `CreateOrderAsync_COD_PublishesBothEvents`
COD is a special case: payment is considered "completed" immediately (no online verification step). The test asserts that BOTH `PaymentCreatedEvent` AND `PaymentCompletedEvent` are published in a single `CreateOrderAsync` call. It also verifies the `PaymentCompletedEvent` payload has `PaymentMethod = "COD"` and the correct `CorrelationId`.

#### Guard Tests (5 tests):
| Test | Exception | Condition |
|---|---|---|
| `ShipmentNotFound` | `KeyNotFoundException` | HTTP 404 from ShipmentService |
| `WrongOwner` | `UnauthorizedAccessException` | `shipment.CustomerId != authenticatedUserId` |
| `AlreadyPaid` | `InvalidOperationException` | Existing payment with `PaymentStatus.Paid` |
| `CODAlreadyRegistered` | `InvalidOperationException` | Existing COD payment |
| `OnlineAlreadyInitiated` | `InvalidOperationException` | Existing Online/Pending payment |
| `NoToken` | `UnauthorizedAccessException` | `Unauthenticated()` context |

**Why test all these guards?** Each guard protects a real business rule. Missing any one of them could allow double-charging a customer or bypassing ownership checks.

---

### `PaymentServiceTests_GetStatus.cs` — 9 Tests

Tests two methods: `GetByShipmentIdAsync` and `PaymentStatusAsync`.

#### `GetByShipmentIdAsync` tests:
The service returns different `Message` strings based on `PaymentStatus` + `PaymentMethod` combination. Tests verify each branch of the switch expression:

```csharp
var message = payment.PaymentStatus switch
{
    PaymentStatus.Paid => "Payment completed successfully.",
    PaymentStatus.Pending when payment.PaymentMethod == PaymentMethod.COD => "COD registered...",
    PaymentStatus.Pending => "Payment initiated. Please complete payment.",
    PaymentStatus.Failed => "Payment failed. Please try again.",
    _ => null
};
```

This is a **decision table test** — each row in the switch is covered by a dedicated test.

#### `PaymentStatusAsync` tests:
The method accepts a `PaymentStatusRequest` with three optional lookup fields: `RazorpayOrderId`, `ShipmentId`, `TrackingNumber`. Tests verify that each lookup path works independently:
- `ByOrderId` → calls `GetByOrderIdAsync`
- `ByShipmentId` → calls `GetByShipmentIdAsync`
- `ByTrackingNumber` → calls `GetByTrackingNumberAsync`

This tests the **priority chain** in the service: OrderId is checked first, then ShipmentId, then TrackingNumber.

---

### `PaymentServiceTests_VerifyPayment.cs` — 10 Tests

This is the most complex test class because `VerifyPaymentAsync` has two distinct code paths: success (valid OrderId) and failure (invalid OrderId with side effects).

#### Happy Path Tests (3 tests):
1. **`ReturnsSuccessResponse`** — verifies the response DTO has `PaymentStatus = "Paid"` and `Message = "Payment successful!"`.
2. **`MarksPaymentAsPaid`** — uses a real `ShipmentPayment` object (not a mock) and asserts that after `VerifyPaymentAsync`, the entity's `PaymentStatus`, `RazorpayPaymentId`, `RazorpaySignature`, and `PaidAt` are all mutated correctly. This tests the **entity mutation** side effect.
3. **`PublishesPaymentCompletedEvent`** — asserts the event payload: `ShipmentId`, `CorrelationId`, `PaymentMethod = "Online"`, `PaymentStatus = "Paid"`.

#### Failure Path Tests (4 tests):
When `GetByOrderIdAsync` returns null (invalid OrderId), the service:
1. Looks up the existing payment by ShipmentId
2. Marks it as `Failed`
3. Saves to DB
4. Publishes `PaymentFailedEvent`
5. Throws `KeyNotFoundException`

Each of these side effects is tested independently:
- `InvalidOrderId_ThrowsKeyNotFoundException` — verifies the exception
- `InvalidOrderId_MarksPaymentAsFailed` — verifies the entity mutation (catches the exception to inspect state)
- `InvalidOrderId_PublishesPaymentFailedEvent` — verifies the event payload including `Reason` containing the bad OrderId
- `InvalidOrderId_StillPublishesEvent_WhenNoSagaCorrelation` — edge case: if no saga correlation exists, `CorrelationId` should be `Guid.Empty` (not throw)

#### Guard Tests (3 tests):
- `WrongOwner` — `payment.CustomerId != authenticatedUserId` → `UnauthorizedAccessException`
- `AlreadyPaid` — `payment.PaymentStatus == Paid` → `InvalidOperationException("already verified")`
- `NoToken` — unauthenticated context → `UnauthorizedAccessException`

---

## Integration Tests: Deep Dive

### `PaymentControllerIntegrationTests.cs` — 9 Tests

**Setup:** Uses `WebApplicationFactory<Program>` with a custom `ConfigureServices` override that:
1. Removes all SQL Server EF Core registrations
2. Replaces with `UseInMemoryDatabase` (unique DB per test class via `Guid.NewGuid()`)
3. Removes all MassTransit/RabbitMQ registrations
4. Replaces with `UsingInMemory` MassTransit transport
5. Removes health check registrations that require real infrastructure

**Why remove and replace instead of just adding?** ASP.NET Core's DI container allows multiple registrations of the same interface. If you just add a new `DbContextOptions`, the original SQL Server one is still there and will be used. You must explicitly remove the old registrations first.

```csharp
var toRemove = services.Where(d =>
    d.ServiceType == typeof(DbContextOptions<PaymentDbContext>) || ...
).ToList();
foreach (var d in toRemove) services.Remove(d);
```

**`CreateAuthenticatedClient()`:** Creates an `HttpClient` with a real JWT token in the `Authorization` header. The token is generated by `TestJwtHelper` using the same secret as the test environment's `appsettings.json`.

#### Authentication Tests (4 tests):
All four payment endpoints are tested without a token. Each must return `401 Unauthorized`. This verifies that the `[Authorize]` attribute on `PaymentController` is correctly applied and that the JWT middleware is wired up.

#### Not Found Tests (3 tests):
With a valid token, requests for non-existent resources (shipmentId=99999, orderId="order_DOESNOTEXIST") must return `404 Not Found`. This verifies that `ExceptionMiddleware` correctly maps `KeyNotFoundException` → HTTP 404.

#### Route Existence Test (1 test):
`POST_CreateOrder_RouteExists_DoesNotReturn404` — sends a request with a non-existent shipmentId (99999) and asserts the response is NOT 404. The expected result is a 4xx or 5xx error (the shipment won't be found), but the important thing is the route itself is registered. This guards against accidental route removal.

---

## Key Technologies & Libraries Used

| Library | Version | Why it was chosen |
|---|---|---|
| **xUnit** | 2.9.3 | The de-facto standard for .NET unit testing. Supports parallel test execution, `IDisposable` for cleanup, and `[Fact]`/`[Theory]` attributes. |
| **Moq** | 4.20.72 | The most widely used .NET mocking library. Supports `Mock<T>`, `Setup`, `Verify`, `Callback`, and `Protected()` for mocking non-public members. |
| **FluentAssertions** | 8.9.0 | Provides a readable, English-like assertion API (`result.Should().Be(...)`, `act.Should().ThrowAsync<T>()`). Makes test failures self-documenting. |
| **Microsoft.AspNetCore.Mvc.Testing** | 10.0.5 | Provides `WebApplicationFactory<TEntryPoint>` for in-process integration testing of ASP.NET Core apps without a real HTTP server. |
| **Microsoft.EntityFrameworkCore.InMemory** | 10.0.5 | In-memory EF Core provider for integration tests. Eliminates the need for a real SQL Server instance. |
| **System.IdentityModel.Tokens.Jwt** | 8.17.0 | Used by `TestJwtHelper` to generate real signed JWT tokens for integration test authentication. |
| **Microsoft.Extensions.Logging.Abstractions** | 10.0.5 | Provides `NullLogger<T>` — a no-op logger that satisfies constructor injection without producing output. |

---

## Data Flow Examples

### End-to-End: Online Payment Creation (Unit Test Flow)

```
Test calls: service.CreateOrderAsync(new CreateOrderRequest(ShipmentId=26, PaymentMethod.Online))
    │
    ├─ MockHttpContext.WithUserId(29) → userId = 29 extracted from claims
    │
    ├─ MockHttpClientFactory.WithResponse(DefaultShipment()) → GET api/shipments/26 → 200 OK
    │   └─ ShipmentDTOs { Id=26, CustomerId=29, ShippingRate=200 }
    │
    ├─ _paymentRepo.GetByShipmentIdAsync(26) → null (no existing payment)
    │
    ├─ _sagaRepo.GetByShipmentIdAsync(26) → ShipmentSagaCorrelation { CorrelationId=<guid> }
    │
    ├─ ShipmentPayment entity created:
    │   { ShipmentId=26, CustomerId=29, Amount=200, Status=Pending, RazorpayOrderId="order_MOCK_..." }
    │
    ├─ _paymentRepo.AddAsync(payment) → captured by Callback in SavesCorrectPaymentEntity test
    ├─ _uow.SaveChangesAsync() → returns 1
    │
    ├─ _publisher.Publish(PaymentCreatedEvent { ShipmentId=26, ... })
    │
    └─ Returns PaymentResponse { PaymentMethod="Online", PaymentStatus="Pending", RazorpayOrderId="order_MOCK_..." }
```

### End-to-End: Payment Verification Failure (Unit Test Flow)

```
Test calls: service.VerifyPaymentAsync(new VerifyPaymentRequest { RazorpayOrderId="order_WRONG", ShipmentId=26 })
    │
    ├─ MockHttpContext.WithUserId(29) → userId = 29
    │
    ├─ _paymentRepo.GetByOrderIdAsync("order_WRONG") → null  ← FAILURE BRANCH
    │
    ├─ _paymentRepo.GetByShipmentIdAsync(26) → existing payment (Status=Pending)
    │   └─ existing.PaymentStatus = PaymentStatus.Failed  ← MUTATION
    │   └─ _paymentRepo.Update(existing)
    │   └─ _uow.SaveChangesAsync()
    │
    ├─ _sagaRepo.GetByShipmentIdAsync(26) → correlation
    │
    ├─ _publisher.Publish(PaymentFailedEvent {
    │       CorrelationId=<guid>, ShipmentId=26,
    │       Reason="Invalid Order ID: order_WRONG"
    │   })
    │
    └─ throws KeyNotFoundException("Invalid Order ID 'order_WRONG'. Payment failed.")
```

---

## Interview-Ready Insights

### Likely Interview Questions

**Q: Why do you mock `HttpMessageHandler` instead of `IHttpClientFactory` directly?**
A: `IHttpClientFactory.CreateClient()` returns an `HttpClient`. You can mock the factory to return a real `HttpClient`, but that `HttpClient` still needs a handler to intercept calls. Mocking `HttpMessageHandler.SendAsync` (via Moq's `Protected()` API) intercepts at the lowest level, giving you full control over what the HTTP call returns without any real network activity.

**Q: Why does `MockPublishEndpoint` implement all 10+ overloads of `IPublishEndpoint`?**
A: `IPublishEndpoint` is a MassTransit interface with many generic and non-generic overloads. The C# compiler requires all interface members to be implemented. A hand-written fake is cleaner than a Moq mock here because it also provides assertion helpers (`WasPublished<T>()`, `GetPublished<T>()`) that make test assertions readable.

**Q: Why does the integration test remove EF Core registrations before adding the in-memory one?**
A: ASP.NET Core's DI container is additive — registering a second `DbContextOptions<PaymentDbContext>` doesn't replace the first. The original SQL Server registration would still be resolved. You must explicitly remove the old descriptor before adding the new one.

**Q: What is the `partial class Program {}` at the bottom of `Program.cs` for?**
A: `WebApplicationFactory<Program>` needs to reference the `Program` class from the test project. In .NET 6+ minimal hosting, `Program` is a top-level statement class that is `internal` by default. The `partial class Program {}` declaration makes it accessible to the test assembly.

**Q: Why does `CreateOrderAsync_COD_PublishesBothEvents` call `_publisher.Reset()` at the start?**
A: The `MockPublishEndpoint` is a shared field on the test class. If a previous test published messages, they'd still be in the list. `Reset()` clears the list to ensure the assertion only checks events from the current test.

### Potential Improvements

1. **Signature verification is not tested.** The real Razorpay SDK verifies the HMAC-SHA256 signature of `razorpay_order_id + "|" + razorpay_payment_id`. The current implementation accepts any signature string. A test should verify that an invalid signature is rejected.
2. **No `[Theory]` data-driven tests.** The rate calculation tests in the ShipmentService use individual `[Fact]` methods per type. A `[Theory]` with `[InlineData]` would be more concise and easier to extend.
3. **Integration tests don't seed data.** The integration tests only test 401/403/404 scenarios. Tests for 200 OK responses would require seeding the in-memory database, which could be done via `IServiceScope` in the test setup.
4. **No test for `PaymentStatusAsync` with `TrackingNumber` lookup in integration tests.** The unit tests cover it, but the integration test only covers the `razorpayOrderId` query parameter path.
5. **Coverlet is not configured.** The ShipmentService.Tests project includes `coverlet.collector` but PaymentService.Tests does not. Adding it would enable code coverage reporting in CI.

### Trade-offs Made

- **In-memory EF Core vs. real SQL Server:** In-memory EF doesn't support transactions, raw SQL, or some LINQ operations. The `ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))` call suppresses the warning about ignored transactions. For a production-grade test suite, Testcontainers with a real SQL Server Docker container would be more faithful.
- **Mock Razorpay vs. real Razorpay SDK:** The service generates `"order_MOCK_" + DateTime.Now.Ticks` instead of calling the real Razorpay API. This is a deliberate simplification — it avoids external API dependencies in tests but means the Razorpay integration itself is untested.
- **`NullLogger` vs. asserting log output:** Using `NullLogger` means log messages are never verified. In a more thorough suite, you'd use `Mock<ILogger<T>>` and verify that specific warning/error logs are emitted for failure scenarios.
