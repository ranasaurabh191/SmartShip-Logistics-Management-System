# SmartShip.IdentityService — Identity & Authentication Service

## Overview

The **IdentityService** is the authentication and user-management backbone of the SmartShip platform. It is responsible for **user registration** (with OTP email verification), **JWT-based login**, **password management** (forgot/reset password flows), **admin-side user CRUD**, and **publishing user lifecycle events** to RabbitMQ so other services stay in sync. It owns the `Users` and `OtpVerifications` tables. Every other service that needs to know "who is this user?" relies on the JWT token issued by this service. It runs on **port 5001** and is the only service the Gateway routes to without mandatory authentication (for the `/api/auth/*` endpoints).

---

## Overall Architecture & Design Decisions

### Architecture Pattern: Layered Architecture (4-Layer)

```
API Layer          → Controllers + Middleware (HTTP concerns only)
Core Layer         → Services (interfaces) + DTOs + Validators (business logic)
Domain Layer       → Entities (pure C# POCOs, no framework dependencies)
Infrastructure Layer → EF Core Repositories + DbContext + UnitOfWork
```

**Why Layered over Clean Architecture?**  
Clean Architecture would add additional abstraction layers (Use Cases, Application boundaries) that are overkill for a service of this scope. The Layered approach gives clear separation — controllers never touch the DB, services never depend on HTTP — without the ceremony of additional project boundaries.

**Dependency direction:** API → Core ← Infrastructure. The Core defines interfaces (`IUserRepository`, `IAuthService`); Infrastructure implements them. This means the Core has zero dependency on EF Core or SQL Server — swapping the DB would only touch Infrastructure.

**Communication:**
- **Inbound:** Synchronous REST (JSON over HTTP)
- **Outbound:** Async publish to RabbitMQ via MassTransit (`UserCreatedEvent`, `UserDeletedEvent`)
- **Inbound messaging:** None — IdentityService only *publishes*, it doesn't *consume* from RabbitMQ (except internally, MassTransit publishes over the bus)
- **Internal HTTP calls:** Identity calls its own `/api/auth/internal/user-email/{id}` endpoint for internal consumers (using `X-Internal-Key` header for authentication)

---

## Folder Structure

```
SmartShip.IdentityService/
├── API/
│   ├── Controllers/
│   │   ├── AuthController.cs          # Login, signup, OTP, forgot/reset password
│   │   └── UserController.cs          # Admin CRUD: list, get, update, delete users
│   └── Middleware/
│       └── ExceptionMiddleware.cs     # Global exception → HTTP status code mapping
├── Core/
│   ├── DTOs/                          # Request/Response data transfer objects
│   │   ├── AuthDTOs.cs                # SignupRequest, LoginRequest, TokenResponse, etc.
│   │   ├── OtpDTOs.cs                 # SignupOtpRequest, VerifyOtpRequest
│   │   └── UserDTOs.cs                # UpdateUserRequest, PagedResponse, UserPagedRequest
│   ├── Interfaces/
│   │   ├── Persistence/IUnitOfWork.cs
│   │   ├── Repositories/IUserRepository.cs
│   │   ├── Repositories/IOtpVerificationRepository.cs
│   │   └── Services/IAuthService.cs, IUserService.cs
│   ├── Services/
│   │   ├── AuthService.cs             # JWT generation, OTP logic, BCrypt hashing
│   │   └── UserService.cs             # Admin user management + event publishing
│   └── Validators/
│       ├── AuthValidators.cs          # FluentValidation for SignupRequest, LoginRequest
│       ├── OtpValidators.cs           # FluentValidation for OTP flow
│       └── UserValidators.cs          # FluentValidation for UpdateUserRequest
├── Domain/
│   └── Entities/
│       ├── User.cs                    # User aggregate root
│       └── OtpVerification.cs         # OTP record with expiry + purpose
├── Infrastructure/
│   ├── Data/IdentityDbContext.cs      # EF Core DbContext
│   ├── Persistence/UnitOfWork.cs      # Transaction boundary abstraction
│   └── Repositories/
│       ├── UserRepository.cs          # EF Core user queries including paged search
│       └── OtpVerificationRepository.cs
├── Program.cs                         # Composition root
├── appsettings.json                   # JWT, DB, RabbitMQ, Serilog config
└── Dockerfile                         # Multi-stage build
```

**Why separate `Core/Interfaces` from `Core/Services`?**  
Interfaces are the *contracts*; services are the *implementations*. This enables Moq-based unit testing of controllers and consumers without needing a real database or email server.

---

## API Endpoints / Message Consumers

### `POST /api/auth/signup` — Direct Signup

**Purpose:** Register a user instantly without OTP (admin-created accounts, internal use). The signed-up user receives a welcome email via the NotificationService event flow.

**Request:**
```json
{
  "name": "Saurabh Rana",
  "email": "saurabh@example.com",
  "phone": "9876543210",
  "password": "Secret@123",
  "role": "CUSTOMER"
}
```

**Business logic (step-by-step):**
1. FluentValidation auto-validates the request (name letters-only, email format, 10-digit phone, password complexity with uppercase + lowercase + digit + special char).
2. `AuthController` calls `IAuthService.SignupAsync()`.
3. Check if email already exists (`UserRepository.ExistsByEmailAsync`) — throw `InvalidOperationException` if duplicate.
4. BCrypt hash the password with work factor ~11 (CPU-hard, timing-attack resistant).
5. Create `User` entity with `IsActive = true`, `Role = "CUSTOMER"` (or provided role).
6. `AddAsync` + `SaveChangesAsync` via UnitOfWork (atomic transaction).
7. Publish `UserCreatedEvent` to RabbitMQ → NotificationService sends welcome email.
8. Return `201 Created` with `TokenResponse` (JWT access token).

**Response:**
```json
{ "token": "eyJhbGci...", "message": "Signup successful." }
```

**Why return a token immediately?** UX decision — no need for a separate login step after signup. The user is authenticated from the moment of account creation.

---

### `POST /api/auth/signup/request-otp` — OTP-based Signup (Step 1)

**Purpose:** Initiates email OTP verification flow. User sends their details; an OTP is emailed to them. Used for customer self-registration to ensure email ownership.

**Business logic:**
1. Validate the request (same rules as direct signup).
2. Check email doesn't already exist.
3. Generate a cryptographically secure 6-digit OTP: `Random.Shared.Next(100000, 999999).ToString()`.
4. BCrypt hash the OTP (same technique as password hashing — if the DB is breached, raw OTPs aren't exposed).
5. Store `OtpVerification` row with `Purpose = "Signup"`, `ExpiresAt = DateTime.Now + 5 minutes`, `IsUsed = false`.
6. Call `IEmailService.SendOtpEmailAsync()` → fires SMTP email via MailKit with styled HTML.
7. Return `200 OK` with confirmation message.

**Why hash the OTP?** The OTP is a temporary credential. If the `OtpVerifications` table is compromised, raw OTPs would allow attackers to register accounts as anyone. Hashing with BCrypt prevents this.

---

### `POST /api/auth/signup/verify-otp` — OTP-based Signup (Step 2)

**Purpose:** Completes the OTP-based signup. Validates the submitted OTP against the hash, then creates the user account.

**Business logic:**
1. Fetch `OtpVerification` row by email + purpose = "Signup".
2. Check IsUsed == false → reject if already consumed (prevents OTP replay attacks).
3. Check ExpiresAt > DateTime.Now → reject if expired (5-minute window).
4. `BCrypt.Verify(submittedOtp, storedHash)` → reject if mismatch.
5. Mark OTP as `IsUsed = true` and delete the row after verification (cleanup).
6. Create user → publish `UserCreatedEvent` → return JWT token.

**Key security properties:**
- OTPs are single-use (IsUsed flag).
- OTPs expire in 5 minutes.
- OTP hashes are stored, not plaintext.
- Lookup is by email + purpose (can have separate flows for signup vs. forgot-password).

---

### `POST /api/auth/login` — Login

**Purpose:** Authenticate an existing user and return a JWT access token.

**Business logic:**
1. Find user by email (`GetByEmailAsync`).
2. Check `IsActive == true` → reject suspended accounts.
3. `BCrypt.Verify(submittedPassword, storedHash)` → reject if wrong.
4. Generate JWT: `JwtSecurityToken` signed with `HS256` algorithm using the shared `JwtSettings:Key`.
   - Claims: `sub` (userId), `email`, `role`, `name`.
   - Expiry: configured in `appsettings.json` (typically 7 days).
5. Return `200 OK` with `{ "token": "...", "userId": ..., "role": "...", "name": "..." }`.

**Why HS256 (symmetric) and not RS256 (asymmetric)?**  
HS256 is simpler to configure — one shared secret. RS256 would allow downstream services to validate tokens using only the public key (no shared secret), which is the preferred approach when services are owned by different teams. For a single-team system, HS256 is a reasonable trade-off.

---

### `POST /api/auth/forgot-password` & `POST /api/auth/reset-password`

**Purpose:** Two-step password reset flow via email OTP.

**Business logic:**
1. `forgot-password`: Find user by email → generate OTP → store with `Purpose = "ResetPassword"` → send email.
2. `reset-password`: Verify OTP (same logic as signup OTP) → BCrypt hash the new password → update user → mark OTP used.

**Why the same OTP infrastructure for both signup and password reset?**  
Code reuse via the `Purpose` discriminator. One `OtpVerification` table, one `IOtpVerificationRepository`, two distinct flows — DRY principle.

---

### `GET /api/auth/internal/user-exists/{userId}` — Internal Service Call

**Purpose:** Called by ShipmentService to verify a customer ID is valid before creating a shipment. Protected by `X-Internal-Key` header (not Bearer JWT).

**Why an internal key instead of JWT?**  
Service-to-service calls don't have a user context — they're machine-to-machine. Using a pre-shared API key (`InternalApi:ApiKey`) is simpler than creating service accounts with JWTs. The key is injected via `IHttpClientFactory` named client configuration.

---

### `GET /api/auth/internal/user-email/{userId}` — Internal Email Retrieval

**Purpose:** Called by NotificationService consumers to get a user's email address for sending notifications. The NotificationService events carry `CustomerId` (int) but need an actual email address to send emails.

---

### `GET /api/admin/users` — List Users (Paginated)

**Purpose:** Admin endpoint to view all users with filtering, search, and pagination.

**Request Query Params:** `?page=1&pageSize=10&role=CUSTOMER&isActive=true&search=saurabh`

**Business logic:**
1. Auth middleware validates Bearer JWT + checks role = ADMIN (via `[Authorize(Roles = "ADMIN")]`).
2. `UserRepository.GetPagedAsync()` builds a dynamic LINQ query:
   - Filter by `Role` (if provided) — case-normalized to uppercase.
   - Filter by `IsActive` (if provided).
   - Full-text `Contains()` search on `Name` OR `Email`.
   - `OrderByDescending(u => u.CreatedAt)` — newest users first.
   - Server-side pagination: `.Skip((page-1) * pageSize).Take(pageSize)`.
3. Returns `PagedResponse<User>` with `Data`, `TotalCount`, `Page`, `PageSize`.

**Why server-side pagination?** Loading all users client-side for filtering/pagination is not scalable. With thousands of users, SQL pagination (OFFSET/FETCH) is far cheaper.

---

### `DELETE /api/admin/users/{id}` — Delete User (Admin)

**Purpose:** Hard-delete a user and cascade event-driven cleanup across all services.

**Business logic:**
1. Fetch user by ID → 404 if not found.
2. Delete all `OtpVerification` records for this user (referential cleanup within the same transaction).
3. Hard-delete the `User` record.
4. `SaveChangesAsync()`.
5. Publish `UserDeletedEvent` → consumed by Shipment, Payment, Notification services which delete their own related data.

**Why event-driven cascade instead of foreign keys?**  
Cross-service cascading via DB foreign keys is impossible in a microservices architecture (each service has its own database). Event-driven cascade is the microservices-native solution. Each service owns its cleanup.

---

## Core Code Deep Dive

### `Core/Services/AuthService.cs`

**JWT Generation:**
```csharp
var claims = new[]
{
    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
    new Claim(ClaimTypes.Email, user.Email),
    new Claim(ClaimTypes.Role, user.Role),
    new Claim("name", user.Name)
};
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JwtSettings:Key"]!));
var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
var token = new JwtSecurityToken(issuer, audience, claims, expires: DateTime.Now.AddDays(7), signingCredentials: creds);
```

**Why `ClaimTypes.NameIdentifier` for userId?** This is the canonical .NET claim type for a user's unique identifier. Downstream services extract the userId from this claim: `User.FindFirst(ClaimTypes.NameIdentifier)?.Value`.

**BCrypt work factor:** BCrypt is intentionally slow. A work factor of 11 means ~300ms to hash a password — a password cracker trying millions of passwords/second is reduced to thousands per second.

### `Core/Services/UserService.cs`

**`DeleteUserAsync` — Transactional consistency:**
```csharp
// 1. Delete OTP entries first (within same EF context)
foreach (var otp in otpEntries) _otpRepository.Delete(otp);
// 2. Delete user
_userRepository.Delete(user);
// 3. Single SaveChanges — atomic
await _unitOfWork.SaveChangesAsync();
// 4. Publish event AFTER successful commit
await _publisher.Publish(new UserDeletedEvent { ... });
```

**Why publish AFTER save?** If we published the event before saving and then SaveChanges failed, downstream services would delete the user's data but the user record would still exist — a split-brain inconsistency. Publishing after a successful commit ensures at-least-once delivery semantics are preserved. (Note: if the publish fails after save, the event is lost — this is an eventual consistency limitation without a transactional outbox.)

### `Infrastructure/Repositories/UserRepository.cs`

**`GetPagedAsync` — Dynamic LINQ query building:**
```csharp
var query = _context.Users.AsQueryable();
if (!string.IsNullOrWhiteSpace(req.Role))
    query = query.Where(u => u.Role == req.Role.ToUpper());
if (req.IsActive.HasValue)
    query = query.Where(u => u.IsActive == req.IsActive.Value);
if (!string.IsNullOrWhiteSpace(req.Search))
    query = query.Where(u => u.Name.Contains(req.Search) || u.Email.Contains(req.Search));
```

This is the **Specification pattern** (inline). `IQueryable<T>` is a deferred query — none of these `Where()` calls hit the database. Only when `.ToListAsync()` is called does EF Core compose a single optimized SQL query with all predicates. This is far more efficient than loading all users and filtering in memory.

### `Domain/Entities/OtpVerification.cs`

```csharp
public string Purpose { get; set; } = "Signup";
```
The `Purpose` field acts as a discriminator column, enabling a single `OtpVerifications` table to serve multiple flows (Signup, ResetPassword). This is a simple alternative to the Table-Per-Type inheritance pattern.

### `API/Middleware/ExceptionMiddleware.cs`

**Centralized exception handling:**
Maps domain exceptions to HTTP status codes:
- `KeyNotFoundException` → 404 Not Found
- `InvalidOperationException` → 400 Bad Request  
- `UnauthorizedAccessException` → 403 Forbidden
- All others → 500 Internal Server Error (with stack trace in Development only)

**Why middleware instead of `[ExceptionFilter]`?** Middleware covers *all* exceptions pipeline-wide, including those thrown by other middleware. `ExceptionFilter` only covers exceptions within MVC action execution. Middleware is more comprehensive.

---

## Key Technologies & Libraries Used

| Technology | Why Used |
|---|---|
| **Entity Framework Core** | ORM for SQL Server; LINQ-based queries; automatic migrations |
| **BCrypt.Net-Next** | Password and OTP hashing; adaptive work factor prevents brute force |
| **FluentValidation** | Declarative, composable validation rules with clear error messages |
| **MassTransit** | Abstracts RabbitMQ; provides publisher pattern with retry semantics |
| **MailKit / MimeKit** | SMTP email sending; HTML email templates for OTP and notifications |
| **Serilog** | Structured logging with request correlation |
| **Microsoft.IdentityModel.Tokens** | JWT generation and claim management |
| **AspNetCore.HealthChecks.SqlServer, RabbitMQ** | Probes DB and RabbitMQ connectivity for the `/health` endpoint |

---

## Data Flow Examples

### End-to-End OTP Signup Flow

```
Client
  ├── POST /gateway/auth/signup/request-otp
  │   { name, email, phone, password }
  ▼
IdentityService
  ├── 1. FluentValidation validates all fields
  ├── 2. Email uniqueness check (DB query)
  ├── 3. Generate OTP: 647291
  ├── 4. BCrypt.Hash("647291") → stored in OtpVerifications table
  ├── 5. EmailService.SendOtpEmailAsync(email, "647291")
  │       → MailKit SMTP → User's inbox
  └── 6. Return 200 "OTP sent to your email."

Client
  ├── POST /gateway/auth/signup/verify-otp
  │   { email, otp: "647291", name, phone, password }
  ▼
IdentityService
  ├── 1. Fetch OtpVerification by email + purpose=Signup
  ├── 2. BCrypt.Verify("647291", storedHash) → true
  ├── 3. Check IsUsed=false, ExpiresAt > now
  ├── 4. Mark IsUsed=true, insert User record
  ├── 5. SaveChangesAsync()
  ├── 6. Publish UserCreatedEvent → RabbitMQ
  │       → NotificationService sends Welcome email
  └── 7. Return 201 + JWT token
```

### Event-Driven User Deletion

```
Admin
  ├── DELETE /gateway/admin/users/42
  ▼
IdentityService
  ├── Delete OTP entries + User record → SaveChanges
  └── Publish UserDeletedEvent { UserId: 42 }

RabbitMQ
  ├── → ShipmentService: delete all shipments for userId 42
  ├── → PaymentService:  delete all payments for userId 42
  └── → NotificationService: delete all notifications for userId 42
```

---

## Interview-Ready Insights

### Potential Interview Questions

1. **"Why store OTP as a hash rather than plaintext?"**  
   → OTP is a temporary credential. BCrypt prevents offline cracking if the DB is leaked. Same reasoning as password hashing — least-privilege data storage.

2. **"How does the Password Reset flow prevent OTP reuse?"**  
   → `IsUsed` flag set to `true` on first successful verification. Subsequent attempts with the same OTP fail the `IsUsed == false` check.

3. **"How does the IdentityService know which claims to put in the JWT?"**  
   → It hard-codes: `userId`, `email`, `role`, `name`. The `role` claim is critical — the Gateway and downstream services use `[Authorize(Roles="ADMIN")]` to enforce RBAC.

4. **"What happens if RabbitMQ is down when a user is created?"**  
   → `_publisher.Publish()` will throw. With MassTransit's RabbitMQ transport, it attempts reconnection automatically (`AutomaticRecoveryEnabled = true`). If it still fails, the event is lost — no transactional outbox is implemented. This is a known limitation.

5. **"Why does IdentityService use `IEmailService` directly instead of publishing an event?"**  
   → For OTP delivery, the user needs the email *synchronously* during the request. If we published an event for the OTP email, there would be an indeterminate delay before NotificationService processed it. Direct SMTP call guarantees immediate delivery.

### Potential Improvements

- **Transactional Outbox:** Persist events to a local `OutboxMessages` table within the same DB transaction, then a background worker publishes them. Prevents event loss on RabbitMQ downtime.
- **Refresh Tokens:** Current implementation issues only access tokens. Adding refresh tokens (stored in DB) would allow long-lived sessions without extending access token expiry.
- **Account Lockout:** No brute-force protection on login. After N failed attempts, lock the account for M minutes.
- **Asymmetric JWT (RS256):** Allow downstream services to validate tokens without sharing the private key — improves security in multi-team environments.
- **Rate Limiting on OTP Endpoint:** An attacker could spam OTP requests, triggering thousands of emails. Add `AddRateLimiter` on the OTP request endpoint.

### Trade-offs Made

| Decision | Trade-off Made |
|---|---|
| HS256 JWT | Simpler; RS256 would be more secure in cross-team scenarios |
| Hard delete users | Simple; soft delete (IsActive=false) would preserve audit trail |
| BCrypt for OTPs | Consistent security; Argon2id would be more modern |
| Direct SMTP for OTP | Synchronous reliability; message queue approach would be more resilient |
