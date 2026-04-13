# SmartShip.PaymentService — Payment Processing Service

## Overview

The **PaymentService** handles all financial transactions in the SmartShip platform. It integrates with **Razorpay** (India's leading payment gateway) to process online card/UPI/netbanking payments and also supports **Cash on Delivery (COD)** workflows. Its core responsibility is: creating payment orders, verifying payment signatures (HMAC integrity check), tracking payment status, and publishing payment lifecycle events (`PaymentCreatedEvent`, `PaymentCompletedEvent`, `PaymentFailedEvent`, `PaymentRefundedEvent`) to RabbitMQ — events that drive the Saga orchestrator in ShipmentService and the notification pipeline in NotificationService. It runs on **port 5005** and manages two tables: `ShipmentPayments` (the payment records) and `ShipmentSagaCorrelations` (lookup table mapping ShipmentId → CorrelationId for Saga routing).

---

## Overall Architecture & Design Decisions

### Architecture Pattern: Layered Architecture + Event Publisher

```
API Layer           → PaymentController (REST endpoints)
Core Layer          → IPaymentService + PaymentService (business logic + Razorpay SDK calls)
Domain Layer        → ShipmentPayment entity, ShipmentSagaCorrelation, PaymentMethod/Status enums
Infrastructure Layer → PaymentDbContext (EF Core) + Repositories + UnitOfWork
                       Messaging/Consumers (ShipmentCreatedConsumer, UserDeletedConsumer)
```

**Why does PaymentService maintain a `ShipmentSagaCorrelations` table?**  
The Saga in ShipmentService is identified by a `CorrelationId` (Guid). However, the PaymentService only knows a `ShipmentId` (int) when a customer pays. Without a mapping table, the PaymentService cannot include the correct `CorrelationId` in its `PaymentCompletedEvent`, meaning the Saga won't advance. The `ShipmentSagaCorrelation` table (`ShipmentId` → `CorrelationId`) is populated when `ShipmentCreatedEvent` arrives, and queried when `PaymentCompletedEvent` needs to be published. This is the **Correlation ID pattern** in action.

**Communication:**
- **Inbound REST:** `create-order`, `verify`, `payment-status`, `shipment/{id}`.
- **Inbound Events:** `ShipmentCreatedEvent` (stores CorrelationId), `UserDeletedEvent` (cascade cleanup).
- **Outbound Events:** `PaymentCreatedEvent`, `PaymentCompletedEvent`, `PaymentFailedEvent`, `PaymentRefundedEvent`.

---

## Folder Structure

```
SmartShip.PaymentService/
├── API/
│   ├── Controllers/
│   │   └── PaymentController.cs         # 4 primary payment endpoints
│   └── Middleware/
│       └── ExceptionMiddleware.cs
├── Core/
│   ├── DTOs/
│   │   ├── CreateOrderRequest.cs        # { ShipmentId, TrackingNumber, Amount, PaymentMethod }
│   │   ├── VerifyPaymentRequest.cs      # { RazorpayOrderId, RazorpayPaymentId, Signature, ShipmentId }
│   │   ├── PaymentStatusRequest.cs      # One of: { RazorpayOrderId } | { ShipmentId } | { TrackingNumber }
│   │   └── PaymentResponse.cs           # Unified response for all payment ops
│   ├── Interfaces/
│   │   ├── Repositories/IPaymentRepository.cs, ISagaCorrelationRepository.cs
│   │   ├── Services/IPaymentService.cs
│   │   └── Persistence/IUnitOfWork.cs
│   └── Services/
│       └── PaymentService.cs            # Razorpay integration + saga event publishing
├── Domain/
│   ├── Entities/
│   │   ├── ShipmentPayment.cs           # Payment record with Razorpay IDs
│   │   └── ShipmentSagaCorrelation.cs   # ShipmentId ↔ CorrelationId mapping table
│   └── Entities/Enums/
│       ├── PaymentMethod.cs             # COD, Online
│       └── PaymentStatus.cs             # Pending, Paid, Failed, Refunded
├── Infrastructure/
│   ├── Data/PaymentDbContext.cs
│   ├── Messaging/Consumers/
│   │   ├── ShipmentCreatedConsumer.cs   # Stores CorrelationId mapping on shipment creation
│   │   └── UserDeletedConsumer.cs       # Cascade-delete payments + correlations on user deletion
│   ├── Persistence/UnitOfWork.cs
│   └── Repositories/
│       ├── PaymentRepository.cs         # Lookups by ShipmentId, OrderId, TrackingNumber
│       └── SagaCorrelationRepository.cs # Lookup CorrelationId by ShipmentId
├── Program.cs                           # Razorpay config + MassTransit + JWT
└── appsettings.json                     # Razorpay Key/Secret, DB, RabbitMQ
```

---

## API Endpoints / Message Consumers

### `POST /api/payment/create-order` — Create Payment Order

**Auth:** Bearer JWT (CUSTOMER or ADMIN)

**Purpose:** This is the entry point for the payment flow. The customer submits their shipment details and payment method. For `Online` payments, a Razorpay order is created and the `orderId` is returned for the frontend to launch the Razorpay checkout widget. For `COD`, no external order is created.

**Request:**
```json
{
  "shipmentId": 42,
  "trackingNumber": "SS20260413154230001",
  "amount": 175.50,
  "paymentMethod": "Online"
}
```

**Business logic (step-by-step):**
1. Extract `customerId` from JWT claim.
2. Check for existing payment: `PaymentRepository.GetByShipmentIdAsync(shipmentId)`. If already exists → throw `InvalidOperationException("Payment already created for this shipment.")`.
3. **For `Online` payment:**
   a. Call Razorpay API: `POST https://api.razorpay.com/v1/orders` with `{ amount: amountInPaise, currency: "INR", receipt: trackingNumber }`.
   b. Razorpay returns `{ id: "order_xxxx", amount: 17550, currency: "INR", ... }`.
   c. Create `ShipmentPayment` with `RazorpayOrderId = "order_xxxx"`, `Status = Pending`.
4. **For `COD` payment:**
   a. Create `ShipmentPayment` with `PaymentMethod = COD`, `Status = Pending` (remains pending until delivery).
   b. No external API call needed.
5. Persist payment via UoW.
6. Publish `PaymentCreatedEvent` → TrackingService creates "PaymentInitiated" tracking entry → NotificationService sends "Payment Initiated" email.
7. Return `PaymentResponse` with `RazorpayOrderId` (for Online) or COD confirmation.

**Response:**
```json
{
  "id": 1,
  "shipmentId": 42,
  "trackingNumber": "SS20260413154230001",
  "amount": 175.50,
  "paymentMethod": "Online",
  "paymentStatus": "Pending",
  "razorpayOrderId": "order_PYzJcDQkTM7Pxa",
  "message": "Payment order created. Complete payment on Razorpay."
}
```

**Why `amountInPaise`?** Razorpay's API accepts amounts in the smallest currency unit. ₹175.50 = 17550 paise. Integer math prevents floating-point precision errors in financial calculations.

---

### `POST /api/payment/verify` — Verify Razorpay Payment

**Auth:** Bearer JWT (CUSTOMER)

**Purpose:** After the customer completes the Razorpay checkout in the browser, Razorpay returns three values: `razorpay_order_id`, `razorpay_payment_id`, and `razorpay_signature`. The frontend sends these to this endpoint. The backend verifies the signature and marks payment as Paid.

**Request:**
```json
{
  "razorpayOrderId": "order_PYzJcDQkTM7Pxa",
  "razorpayPaymentId": "pay_PYzK0zFkT5f8OP",
  "signature": "abc123def456...",
  "shipmentId": 42
}
```

**Business logic (step-by-step):**
1. Extract `authenticatedUserId` from JWT.
2. Fetch payment: `GetByOrderAndShipmentAsync(orderId, shipmentId)`.
   - Not found → publish `PaymentFailedEvent` → throw `KeyNotFoundException` → Saga will cancel the shipment.
3. **Ownership check:** `payment.CustomerId != authenticatedUserId` → throw `UnauthorizedAccessException`. (Prevents user A from verifying user B's payment.)
4. **Idempotency check:** `payment.PaymentStatus == Paid` → throw to prevent double-verify.
5. **Razorpay Signature Verification:**
   ```csharp
   var payload = $"{razorpayOrderId}|{razorpayPaymentId}";
   var expectedSignature = HMACSHA256(payload, razorpaySecretKey);
   if (expectedSignature != submittedSignature) → payment failed
   ```
   This is the **critical security check**. Without it, any malicious client could send fake payment IDs and claim successful payment. The HMAC signature proves Razorpay actually processed this payment — only Razorpay knows the secret key used to sign.
6. Update: `Status = Paid`, store `RazorpayPaymentId`, `RazorpaySignature`, `PaidAt = DateTime.Now`.
7. Lookup `CorrelationId` from `SagaCorrelationRepository`.
8. Publish `PaymentCompletedEvent { CorrelationId, ShipmentId, ... }` → Saga transitions PaymentPending → Confirmed.
9. Return success response.

**The Razorpay HMAC verification — interview critical:**
```csharp
var keyBytes = Encoding.UTF8.GetBytes(razorpaySecretKey);
var payload = $"{orderId}|{paymentId}";
using var hmac = new HMACSHA256(keyBytes);
var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
var expectedSignature = Convert.ToHexString(hash).ToLower();
// Compare with submitted signature
```
**Why HMAC and not just checking the payment ID with Razorpay's API?**  
`HMAC` verification is synchronous, offline, and crypto-secure. Calling Razorpay's `/payments/{id}` verification API would add latency, require network call, and create a dependency on Razorpay's availability. HMAC allows the server to verify authenticity instantly.

---

### `GET /api/payment/payment-status` — Query Payment Status

**Auth:** Bearer JWT

**Purpose:** Flexible status lookup by any identifier — Customer can check by OrderId, ShipmentId, or TrackingNumber.

**Request query params:** `?razorpayOrderId=order_xxx` OR `?shipmentId=42` OR `?trackingNumber=SS...`

**Business logic:**
1. Priority: `RazorpayOrderId` → `ShipmentId` → `TrackingNumber`.
2. Fetch payment by whichever identifier is provided.
3. Generate human-readable status message:
   ```csharp
   var message = payment.PaymentStatus switch {
       Paid     => "Payment completed successfully.",
       Pending when COD => "COD registered. Pay on delivery.",
       Pending          => "Payment initiated. Please complete payment.",
       Failed           => "Payment failed. Please try again.",
       _                => ""
   };
   ```
4. Return full `PaymentResponse`.

**Why multiple lookup keys?** Different parts of the system have access to different identifiers:
- The customer has the `razorpayOrderId` from the checkout session.
- The tracking page has the `trackingNumber`.
- The shipment detail page has the `shipmentId`.
Supporting all three avoids unnecessary additional API calls.

---

### `GET /api/payment/shipment/{shipmentId}` — Get Payment for Shipment

**Auth:** Bearer JWT

**Purpose:** Called internally by ShipmentService's `SchedulePickupAsync` (HTTP client call) to check if payment is complete before allowing pickup scheduling.

Returns `PaymentResponse` for the given `shipmentId`. ShipmentService checks `PaymentStatus == "Paid"` and `PaymentMethod` from this response.

---

## Message Consumers

### `ShipmentCreatedConsumer`

**Event:** `ShipmentCreatedEvent`  
**Purpose:** Stores the `CorrelationId → ShipmentId` mapping.

**Business logic:**
```csharp
var existing = await _context.SagaCorrelations
    .FirstOrDefaultAsync(x => x.ShipmentId == msg.ShipmentId);
if (existing != null) return; // Idempotency — skip if already stored

_context.SagaCorrelations.Add(new ShipmentSagaCorrelation {
    ShipmentId = msg.ShipmentId,
    CustomerId = msg.CustomerId,
    CorrelationId = msg.CorrelationId
});
await _context.SaveChangesAsync();
```

**Why the idempotency check?** RabbitMQ at-least-once delivery can cause this consumer to fire twice for the same event. The check on `ShipmentId` existence prevents duplicate correlation records with unique constraint violations.

---

### `UserDeletedConsumer`

**Event:** `UserDeletedEvent`  
**Purpose:** GDPR-compliant cascade cleanup — deletes all payment records and saga correlations for the deleted user.

**Business logic:**
```csharp
var payments = await _db.Payments.Where(p => p.CustomerId == userId).ToListAsync();
_db.Payments.RemoveRange(payments);
await _db.SaveChangesAsync();

var saga = await _db.SagaCorrelations.Where(s => s.CustomerId == userId).ToListAsync();
_db.SagaCorrelations.RemoveRange(saga);
await _db.SaveChangesAsync();
```

Note: Two separate `SaveChangesAsync()` calls (not one). This is intentional — if saga deletion fails, payments are already cleaned up (partial cleanup logged), rather than both failing atomically. A transactional outbox would handle this better.

---

## Core Code Deep Dive

### `Domain/Entities/ShipmentPayment.cs`

```csharp
public class ShipmentPayment
{
    public int Id { get; set; }
    public int ShipmentId { get; set; }
    public string TrackingNumber { get; set; } = "";
    public int CustomerId { get; set; }
    public decimal Amount { get; set; }            // Always in ₹ (INR), converted to paise for Razorpay
    public PaymentMethod PaymentMethod { get; set; }
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
    public string? RazorpayOrderId { get; set; }   // null for COD
    public string? RazorpayPaymentId { get; set; } // null until verified
    public string? RazorpaySignature { get; set; } // null until verified
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? PaidAt { get; set; }          // null until payment verified
    public Guid SagaCorrelationId { get; set; }
    public DateTime? RefundedAt { get; set; }      // null unless refunded
}
```

**Design notes:**
- `TrackingNumber` is denormalized (stored in payment though it's in Shipment table). This avoids a JOIN when TrackingService needs to create tracking events from payment events.
- All Razorpay fields are nullable — COD payments never use them.
- `SagaCorrelationId` on the entity itself provides a direct FK reference to the saga without always going through `SagaCorrelations` table.

### `Domain/Entities/ShipmentSagaCorrelation.cs`

```csharp
public class ShipmentSagaCorrelation
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int ShipmentId { get; set; }   // PK — not auto-generated
    public int CustomerId { get; set; }
    public Guid CorrelationId { get; set; }
}
```

`[DatabaseGenerated(DatabaseGeneratedOption.None)]` tells EF Core that `ShipmentId` is manually assigned (not an identity/sequence). Without this, EF Core would try to auto-generate the ID via `IDENTITY` in SQL Server, causing an exception since we're explicitly setting it from the event.

---

## Key Technologies & Libraries Used

| Technology | Why Used |
|---|---|
| **Razorpay .NET SDK** | Indian payment gateway with UPI/card/netbanking; HMAC signature for verification |
| **HMACSHA256** | Cryptographic signature verification for Razorpay webhooks/callbacks |
| **MassTransit + RabbitMQ** | Publish payment lifecycle events + consume shipment/user events |
| **Entity Framework Core** | ORM; two aggregate roots in PaymentDbContext |
| **Microsoft.AspNetCore.Authentication.JwtBearer** | Extract customerId from JWT claims |
| **Serilog** | Request and consumer logging |

---

## Data Flow Examples

### Online Payment Flow (Complete)

```
1. Customer: POST /gateway/payment/create-order
   { shipmentId: 42, amount: 175.50, paymentMethod: "Online" }
   
   PaymentService:
   ├── Calls Razorpay: POST /v1/orders → { id: "order_xxx" }
   ├── Creates ShipmentPayment { Status: Pending, RazorpayOrderId: "order_xxx" }
   ├── Saves to DB
   ├── Publishes PaymentCreatedEvent
   └── Returns { razorpayOrderId: "order_xxx" }

2. Frontend: Shows Razorpay checkout modal using "order_xxx"
   Customer fills card/UPI details and submits

3. Razorpay: Redirects/calls frontend with:
   { razorpay_order_id, razorpay_payment_id, razorpay_signature }

4. Customer: POST /gateway/payment/verify
   { razorpayOrderId, razorpayPaymentId, signature }
   
   PaymentService:
   ├── Fetch payment by OrderId + ShipmentId
   ├── Verify ownership (CustomerId from JWT == payment.CustomerId)
   ├── HMAC verify: SHA256(orderId + "|" + paymentId, secret) == submitted signature ✅
   ├── Update: Status=Paid, PaidAt=now
   ├── Lookup CorrelationId from SagaCorrelations
   ├── Publish PaymentCompletedEvent { CorrelationId, ShipmentId, ... }
   └── Return success

5. Saga (in ShipmentService):
   ├── Receives PaymentCompletedEvent
   └── State: PaymentPending → Confirmed

6. Customer: PATCH /gateway/shipments/{id}/schedule-pickup
   ShipmentService:
   ├── HTTP GET /api/payment/shipment/42 → { PaymentStatus: "Paid" }
   └── Allows pickup scheduling
```

### COD Payment Flow

```
1. POST /gateway/payment/create-order { paymentMethod: "COD" }
   PaymentService:
   ├── No Razorpay call needed
   ├── Creates Payment { Status: Pending, Method: COD }
   └── Publishes PaymentCreatedEvent

2. // No verify step for COD — payment collected on delivery

3. Customer schedules pickup:
   ShipmentService checks payment:
   ├── PaymentMethod == "COD" → skip payment verification
   └── Allow pickup scheduling

4. After delivery:
   Admin updates status to Delivered → ShipmentDeliveredEvent published
   (COD payment remains "Pending" — collected physically)
```

### Payment Failure → Automatic Shipment Cancellation

```
1. Customer calls POST /verify with wrong/expired data
   PaymentService:
   ├── HMAC mismatch or payment not found
   ├── Looks up CorrelationId from SagaCorrelations
   └── Publishes PaymentFailedEvent { CorrelationId, Reason: "Invalid signature" }

2. Saga (ShipmentService):
   ├── Receives PaymentFailedEvent
   ├── Publishes CancelShipmentCommand { ShipmentId, Reason }
   └── State: PaymentPending → Cancelled

3. CancelShipmentConsumer (ShipmentService):
   ├── Finds shipment, Status == Draft
   ├── Sets Status = Cancelled, Notes = "Auto-cancelled: Invalid signature"
   └── Publishes ShipmentCancelledEvent

4. NotificationService: Sends "Shipment Cancelled" email
```

---

## Interview-Ready Insights

### Potential Interview Questions

1. **"How does Razorpay signature verification work? Why is it important?"**
   → HMAC-SHA256 over `orderId|paymentId` using Razorpay's secret key. Without it, a client could submit any fake paymentId and claim successful payment. The HMAC proves Razorpay signed this transaction.

2. **"Why does PaymentService have a `ShipmentSagaCorrelations` table?"**
   → The Saga uses `CorrelationId` (Guid) but payment events only have `ShipmentId` (int). The mapping table bridges this gap, enabling PaymentService to include the correct `CorrelationId` in payment events so the Saga advances correctly.

3. **"What happens if `PaymentCompletedEvent` is published but RabbitMQ is down?"**
   → The event is lost. The Saga stays in `PaymentPending`. Customer would be charged but shipment wouldn't advance. This is the transactional outbox problem — a known limitation without the outbox pattern.

4. **"How does COD differ from Online in the payment flow?"**
   → COD: no Razorpay order, no verification step, pickup allowed without payment confirmation. Online: Razorpay order required, HMAC verify required before pickup. The `SchedulePickupAsync` in ShipmentService explicitly branches on `PaymentMethod`.

5. **"What is `[DatabaseGenerated(DatabaseGeneratedOption.None)]` on `ShipmentSagaCorrelation.ShipmentId`?"**
   → Prevents SQL Server `IDENTITY` column generation. Since `ShipmentId` comes from the event, it's externally assigned. Without this attribute, EF would ignore the value and let SQL generate it, resulting in wrong IDs.

### Potential Improvements

- **Transactional Outbox:** Publish events atomically within the same DB transaction. Prevents "payment saved but event lost" scenario.
- **Webhook from Razorpay:** Instead of client-side verification, use Razorpay's server-to-server webhook to guarantee delivery. Adds reliability.
- **Refund Implementation:** `PaymentRefundedEvent` is published but the actual Razorpay refund API call (`POST /refunds`) isn't implemented. This is a placeholder.
- **Payment Expiry:** If a customer creates an order but never pays, the payment remains `Pending` + `Status.Draft` forever. Implement a background job to expire stale pending payments after 24 hours.
- **Audit Log:** Store every payment state transition with timestamps (e.g., `Pending → Paid at 10:35AM`).

### Trade-offs Made

| Decision | Trade-off |
|---|---|
| Client-side Razorpay callback | Simpler frontend; must verify HMAC to prevent fraud |
| Separate SagaCorrelations table | Solves ID mismatch; extra table to maintain |
| Two SaveChangesAsync in UserDeletedConsumer | Simpler; non-atomic; partial cleanup on failure |
| Decimal for Amount | Prevents floating-point issues; converts to paise for Razorpay |
