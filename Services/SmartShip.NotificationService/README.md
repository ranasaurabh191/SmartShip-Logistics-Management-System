# SmartShip.NotificationService — Notification & Email Service

## Overview

The **NotificationService** is the communication backbone of the SmartShip platform. It is responsible for sending **transactional HTML emails** to customers at every key milestone of their shipment journey (account created, shipment created, status changed, payment completed, shipment delivered, shipment cancelled) and for **persisting a notification history** so users can review past communications. It is a **pure consumer service** — it has no domain logic of its own, no business decisions to make, and no outbound HTTP calls to initiate workflows. It listens to RabbitMQ events, retrieves the user's email address from IdentityService via an internal HTTP call, sends the email via SMTP (MailKit + Gmail), and stores the notification record in its own database. It runs on **port 5006**.

---

## Overall Architecture & Design Decisions

### Architecture Pattern: Layered Architecture — Event-Driven Email Consumer

```
API Layer           → NotificationController (REST: query notification history)
Core Layer          → INotificationService + NotificationService (send + save logic)
                       IEmailService interface
Domain Layer        → Notification entity
Infrastructure Layer → NotificationDbContext + NotificationRepository + UnitOfWork
                       EmailService (MailKit SMTP)
                       Messaging/Consumers (8 event consumers)
                       Helpers/ConsumerHelper.cs (shared email-lookup logic)
```

**Why is NotificationService a separate microservice?**
Email sending is inherently unreliable (SMTP servers time out, rate limits, network issues). Decoupling it from business services means: if email sending fails, the shipment creation doesn't fail. An email failure is logged but doesn't roll back business operations. This is the **fire-and-forget reliability pattern** — decoupled via message broker.

**Why does NotificationService call IdentityService for email addresses (instead of events carrying the email)?**  
Most events (`ShipmentCreatedEvent`, `ShipmentStatusUpdatedEvent`) carry only `CustomerId` (int), not the email address. Carrying the email in every event would:
1. Create redundancy — email can change over time, cached email in events would be stale.
2. Create a privacy concern — email addresses propagating through every event topic.

Instead, the consumer calls IdentityService's internal endpoint `/api/auth/internal/user-email/{userId}` on demand using `X-Internal-Key` authentication. This is a **late-binding lookup** — always fetches the current email at notification time.

**Communication:**
- **Inbound Events:** 8 consumer types from RabbitMQ.
- **Inbound REST:** Query notification history (per user, paginated).
- **Outbound HTTP:** GET to IdentityService for email lookup (internal API call).
- **Outbound SMTP:** MailKit to Gmail/SMTP server.

---

## Folder Structure

```
SmartShip.NotificationService/
├── API/
│   ├── Controllers/
│   │   └── NotificationController.cs       # GET /notifications (admin/user history)
│   └── Middleware/
│       └── ExceptionMiddleware.cs
├── Core/
│   ├── DTOs/
│   │   └── NotificationDTOs.cs              # NotificationDto, NotificationPagedRequest
│   ├── Interfaces/
│   │   ├── Repositories/INotificationRepository.cs
│   │   ├── Services/INotificationService.cs
│   │   │                                    # (IEmailService is in NotificationService.Infrastructure.Services)
│   │   └── Persistence/IUnitOfWork.cs
│   └── Services/
│       └── NotificationService.cs           # SendAndSaveAsync: email + persist
├── Domain/
│   └── Entities/
│       └── Notification.cs                  # persisted notification: userId, email, subject, body, type, isEmailSent
├── Infrastructure/
│   ├── Data/NotificationDbContext.cs
│   ├── Helpers/
│   │   └── ConsumerHelper.cs               # Shared GetUserEmailAsync() used by all consumers
│   ├── Messaging/Consumers/
│   │   ├── UserCreatedConsumer.cs           # Welcome email (direct — event carries email)
│   │   ├── UserDeletedConsumer.cs           # Cascade cleanup of notification records
│   │   ├── ShipmentCreatedConsumer.cs       # "Shipment Created" email
│   │   ├── ShipmentDeliveredConsumer.cs     # "Shipment Delivered" email
│   │   ├── ShipmentStatusUpdatedConsumer.cs # "Status Updated" email
│   │   ├── ShipmentCancelledConsumer.cs     # "Shipment Cancelled" email
│   │   ├── PaymentCompletedConsumer.cs      # "Payment Confirmed" email
│   │   └── PaymentFailedConsumer.cs         # "Payment Failed" email
│   ├── Persistence/UnitOfWork.cs
│   └── Repositories/NotificationRepository.cs
│   └── Services/
│       └── EmailService.cs                  # MailKit SMTP implementation
├── Program.cs                               # MassTransit + 8 consumers + MailKit config
└── appsettings.json                         # SMTP credentials, internal API key, RabbitMQ
```

**Why `ConsumerHelper` in Infrastructure/Helpers?**  
8 consumers all need to look up a user's email by `CustomerId`. `ConsumerHelper.GetUserEmailAsync()` is a single static method extracted to prevent duplication across all consumers — a form of **DRY** applied at the infrastructure level. Static helpers in the Infrastructure layer are acceptable because they have no domain concerns and are pure infrastructure utilities.

---

## API Endpoints / Message Consumers

### `GET /api/notifications` — Admin: List All Notifications

**Auth:** Bearer JWT (ADMIN role — enforced in service layer)

**Purpose:** Operations and admin team view of all emails ever sent by the system. Useful for debugging delivery issues.

**Request:** `GET /api/notifications?type=ShipmentCreated&isEmailSent=true&search=saurabh@example.com&page=1&pageSize=20`

**Business logic:**
1. `NotificationRepository.GetPagedAsync(req)` with optional filters:
   - `Type`: exact match (e.g., `"WelcomeEmail"`, `"StatusUpdated"`, `"ShipmentDelivered"`).
   - `IsEmailSent`: filter by whether SMTP succeeded.
   - `Search`: `email` OR `subject` contains search term.
   - Default sort: `OrderByDescending(n => n.CreatedAt)`.

**Response:**
```json
{
  "data": [
    {
      "id": 12,
      "userId": 7,
      "email": "customer@example.com",
      "type": "ShipmentCreated",
      "subject": "Shipment Created - SS20260413154230001",
      "isEmailSent": true,
      "createdAt": "13-Apr-2026 04:00 PM"
    }
  ],
  "totalCount": 87,
  "page": 1,
  "pageSize": 20
}
```

---

### `GET /api/notifications/user/{userId}` — User's Notification History

**Auth:** Bearer JWT (CUSTOMER for own notifications, ADMIN for any user)

**Business logic:**
1. If CUSTOMER: `userId` from JWT must match the path param (prevents customers viewing each other's notifications).
2. `NotificationRepository.GetPagedByUserAsync(userId, req)` — filtered by `UserId`.
3. Supports `?type=ShipmentDelivered&search=tracking&sortOrder=asc`.

---

## Message Consumers — The Core of NotificationService

### `UserCreatedConsumer`

**Event:** `UserCreatedEvent`

**Why this consumer is different from others:**  
The `UserCreatedEvent` carries the `Email` field directly in the event payload (unlike other events that only carry `CustomerId`). This is because at the time of user creation, the email is in the event naturally. No HTTP lookup to IdentityService needed.

```csharp
await _notification.SendAndSaveAsync(
    msg.UserId, msg.Email,          // email from event directly
    type: "WelcomeEmail",
    subject: "Welcome to SmartShip! 🚀",
    body: $"<h2>Hi {msg.Name}, Welcome to SmartShip!</h2>..."
);
```

---

### `ShipmentCreatedConsumer`

**Event:** `ShipmentCreatedEvent`

```csharp
var email = await ConsumerHelper.GetUserEmailAsync(
    _httpClientFactory, _logger, msg.CustomerId, _config);
if (email == null) return;  // Graceful skip if user not found

await _notification.SendAndSaveAsync(
    msg.CustomerId, email,
    type: "ShipmentCreated",
    subject: $"Shipment Created - {msg.TrackingNumber}",
    body: $"""
        <h2>Your Shipment Has Been Created!</h2>
        <p><b>Tracking Number:</b> {msg.TrackingNumber}</p>
        <p><b>From:</b> {msg.SenderCity}</p>
        <p><b>Created At:</b> {msg.CreatedAt:dd-MMM-yyyy hh:mm tt}</p>
        <br/>
        <p>Please complete payment and schedule pickup to proceed.</p>
        <p>- SmartShip Team</p>
    """
);
```

**Pattern: `if (email == null) return;`**  
If the user was deleted between shipment creation and notification processing, `GetUserEmailAsync` returns `null`. Rather than throwing an exception (which would cause RabbitMQ to retry indefinitely), the consumer logs a warning and returns gracefully. This is the **poison message prevention** pattern.

---

### `ShipmentStatusUpdatedConsumer`

**Event:** `ShipmentStatusUpdatedEvent`

**Email content** includes `OldStatus → NewStatus` transition with location. This gives customers precise information:
```html
<p><b>Status:</b> PickedUp → <b>InTransit</b></p>
<p><b>Location:</b> Delhi Distribution Hub</p>
<p><b>Updated At:</b> 13-Apr-2026 05:00 PM</p>
```

---

### `ShipmentDeliveredConsumer`

**Event:** `ShipmentDeliveredEvent`

```html
<h2>Your Shipment Has Been Delivered!</h2>
<p>Thank you for using SmartShip!</p>
```

The "Delivered" notification is the customer's satisfaction moment. Keeping it simple and celebratory is intentional UX design.

---

### `PaymentCompletedConsumer`

**Event:** `PaymentCompletedEvent`

Email content confirms payment verified and shipment is active.

---

### `PaymentFailedConsumer`

**Event:** `PaymentFailedEvent`

Email alerts customer that their payment failed and shipment will be cancelled. Gives them a chance to retry.

---

### `ShipmentCancelledConsumer`

**Event:** `ShipmentCancelledEvent`

Email notifies customer of cancellation with reason.

---

### `UserDeletedConsumer`

**Event:** `UserDeletedEvent`

**Purpose:** GDPR cascade cleanup.

```csharp
var notifications = await _db.Notifications
    .Where(n => n.UserId == userId)
    .ToListAsync();
_db.Notifications.RemoveRange(notifications);
await _db.SaveChangesAsync();
```

Unlike other consumers, this one receives email notifications to **delete** them, not create them. No email is sent for account deletion — the user is gone.

---

## Core Code Deep Dive

### `Core/Services/NotificationService.cs` — `SendAndSaveAsync`

This is the central method called by all consumers:

```csharp
public async Task SendAndSaveAsync(int userId, string email, string type, string subject, string body)
{
    bool emailSent = false;
    try
    {
        await _emailService.SendEmailAsync(email, subject, body);
        emailSent = true;
        _logger.LogInformation("Email sent to {Email} | Type: {Type}", email, type);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Email failed to {Email} | Type: {Type}", email, type);
        // emailSent remains false — still saves notification record
    }
    finally
    {
        var notification = new Notification
        {
            UserId = userId,
            Email = email,
            Type = type,
            Subject = subject,
            Body = body,
            IsEmailSent = emailSent
        };
        await _notificationRepository.AddAsync(notification);
        await _unitOfWork.SaveChangesAsync();
    }
}
```

**Key design decisions:**
1. **`try/catch` around email sending only:** If SMTP fails, the notification record is still saved with `IsEmailSent = false`. This enables: (a) admins to see which emails failed, (b) future retry logic to query `IsEmailSent = false` records and resend.
2. **`finally` for persistence:** The `finally` block ALWAYS saves the notification. Even if the email fails, the record exists. This is critical for audit logging.
3. **No re-throw:** The email failure is absorbed. This prevents the consumer from crashing and triggering RabbitMQ nack/retry, which would cause infinite retry loops on SMTP failures (e.g., rate limiting).

### `Infrastructure/Services/EmailService.cs` — MailKit SMTP

```csharp
public async Task SendEmailAsync(string toEmail, string subject, string body)
{
    var message = new MimeMessage();
    message.From.Add(new MailboxAddress(senderName, senderEmail));
    message.To.Add(MailboxAddress.Parse(toEmail));
    message.Subject = subject;
    message.Body = new TextPart("html") { Text = body };

    using var smtp = new SmtpClient();      // Disposed after send — no connection pooling
    await smtp.ConnectAsync(host, port, SecureSocketOptions.StartTls);
    await smtp.AuthenticateAsync(senderEmail, password);
    await smtp.SendAsync(message);
    await smtp.DisconnectAsync(true);       // true = graceful QUIT command
}
```

**Why `SecureSocketOptions.StartTls`?** Port 587 uses STARTTLS — the connection starts unencrypted then upgrades to TLS. Port 465 uses `SslOnConnect` (implicit TLS). Gmail requires port 587 + STARTTLS.

**Constructor logging:**
```csharp
_logger.LogInformation("SenderEmail: '{Sender}'", _config["EmailSettings:SenderEmail"] ?? "NULL");
_logger.LogInformation("Host: '{Host}'", _config["EmailSettings:Host"] ?? "NULL");
```
The constructor logs all SMTP configuration values on startup. This is defensive debugging — if email never sends in production, the first place to check is "did the config load?" This startup log answers that immediately.

### `Infrastructure/Helpers/ConsumerHelper.cs`

```csharp
public static async Task<string?> GetUserEmailAsync(
    IHttpClientFactory factory, ILogger logger, int userId, IConfiguration config)
{
    var client = factory.CreateClient("IdentityService");
    var response = await client.GetAsync($"/api/auth/internal/user-email/{userId}");
    
    if (!response.IsSuccessStatusCode)
    {
        logger.LogWarning("Could not get email for UserId {UserId}: {Code}", userId, response.StatusCode);
        return null;
    }
    
    var json = await response.Content.ReadFromJsonAsync<EmailResponse>();
    return json?.Email;
}
```

**Why static?** It's a pure utility function with no state. Static methods are simpler than injecting a dedicated helper service into every consumer. The `IHttpClientFactory` and `ILogger` are passed as parameters rather than injected, keeping it stateless.

**Why `IHttpClientFactory` named client `"IdentityService"`?**  
Configured in `Program.cs`:
```csharp
builder.Services.AddHttpClient("IdentityService", c => {
    c.BaseAddress = new Uri(urls["IdentityService"]!);
    c.DefaultRequestHeaders.Add("X-Internal-Key", internalKey);
});
```
The `X-Internal-Key` header is added to every request automatically. Consumers don't need to know the key — just request the named client.

---

## Key Technologies & Libraries Used

| Technology | Why Used |
|---|---|
| **MassTransit + RabbitMQ** | 8 event consumers covering full shipment + payment + user lifecycle |
| **MailKit + MimeKit** | .NET SMTP client; HTML email; STARTTLS for secure sending |
| **IHttpClientFactory (named client)** | HTTP pool management for IdentityService calls; auto-injects X-Internal-Key |
| **Entity Framework Core** | Notification persistence; paged history queries |
| **Serilog** | Consumer-level logging; email success/failure tracking |

---

## Data Flow Examples

### Flow: Full Notification Chain for a New Shipment

```
1. IdentityService publishes UserCreatedEvent { userId: 7, email: "saurabh@x.com", name: "Saurabh" }
   → NotificationService/UserCreatedConsumer
   → SendAndSaveAsync: "Welcome to SmartShip!" email sent
   → Notification record saved (isEmailSent: true)

2. ShipmentService publishes ShipmentCreatedEvent { shipmentId: 42, customerId: 7, trackingNumber: "SS..." }
   → NotificationService/ShipmentCreatedConsumer
   → ConsumerHelper.GetUserEmailAsync(customerId: 7) → HTTP GET → IdentityService → "saurabh@x.com"
   → SendAndSaveAsync: "Shipment Created - SS..." email sent

3. PaymentService publishes PaymentCompletedEvent { shipmentId: 42, customerId: 7 }
   → NotificationService/PaymentCompletedConsumer
   → Email: "Payment Confirmed for SS..."

4. ShipmentService publishes ShipmentStatusUpdatedEvent { customerId: 7, old: "Booked", new: "PickedUp" }
   → Email: "Status Updated: Booked → PickedUp"

5. ShipmentService publishes ShipmentDeliveredEvent { customerId: 7 }
   → Email: "Your Shipment Has Been Delivered!"
```

Customer receives 5 emails throughout their shipment journey — zero configuration required.

### Flow: Email Failure Scenario

```
ShipmentCreatedConsumer.Consume()
  ├── ConsumerHelper.GetUserEmailAsync() → "saurabh@x.com"
  ├── SendAndSaveAsync()
  │   ├── EmailService.SendEmailAsync() → SMTP timeout (Gmail rate limit)
  │   ├── Logs: "Email failed to saurabh@x.com | Type: ShipmentCreated"
  │   └── [finally] Saves Notification { IsEmailSent: false }
  └── Consumer returns (no exception thrown — no RabbitMQ nack)

Admin views: GET /api/notifications?isEmailSent=false
  → Sees failed notifications → can manually retry or investigate SMTP config
```

---

## Interview-Ready Insights

### Potential Interview Questions

1. **"Why does NotificationService call IdentityService for email instead of having email in events?"**
   → Avoids email propagation through every event (privacy), avoids stale email if user updates their address, follows single source of truth principle.

2. **"What is the `ConsumerHelper` and why is it static?"**
   → Shared email-lookup logic used by 7 consumers. Static because it's stateless — accepts all dependencies as parameters. Avoids DI overhead for a simple utilities function.

3. **"What happens if email sending fails? Does the message get requeued?"**
   → No requeue. `SendAndSaveAsync` uses try/catch that absorbs the SMTP exception. The notification is saved with `IsEmailSent = false`. RabbitMQ considers the message successfully consumed. This prevents infinite retry loops on persistent SMTP failures.

4. **"How is the notification history used?"**
   → Admin can view all notifications (including failed ones by filtering `isEmailSent=false`). Could be used to build a retry mechanism for failed emails.

5. **"Why does UserCreatedConsumer use `msg.Email` directly while other consumers call IdentityService?"**
   → `UserCreatedEvent` carries the email because it's available at creation time. Other events (shipment, payment) only carry `CustomerId` — they happen in services that don't own user data. Consistency vs. efficiency trade-off.

### Potential Improvements

- **Email Retry Queue:** Background service that queries `IsEmailSent = false` notifications older than X minutes and retries sending.
- **Email Templates:** Current HTML email is hardcoded strings. A template engine (e.g., Scriban, Handlebars.NET, or Razor views) would enable marketing team to update email copy without code changes.
- **Idempotency:** A consumer could process the same event twice (RabbitMQ at-least-once). Currently, the customer would receive duplicate emails. Add an `ExternalEventId` deduplication check.
- **Multiple Channels:** Currently only email. A proper notification service would support SMS (Twilio), push notifications (Firebase FCM), and in-app notifications — all triggered by the same event consumers.
- **Bounce/Delivery Tracking:** MailKit doesn't receive delivery receipts (bounces). Integrate an email service like SendGrid that provides delivery webhooks.

### Trade-offs Made

| Decision | Trade-off |
|---|---|
| Absorb SMTP exceptions | No retries; failed emails recorded for manual intervention |
| Late-binding email lookup | Always fresh email; extra HTTP call per notification |
| Static ConsumerHelper | Simple; not injectable/mockable (impacts unit testing of consumers) |
| HTML hardcoded in consumers | Fast to implement; hard to update without code changes |
| MailKit direct SMTP | Full control; no vendor lock-in; requires SMTP server management |
