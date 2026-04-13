# SmartShip.Gateway — API Gateway Service

## Overview

The **SmartShip Gateway** is the single entry point for all external traffic in the SmartShip microservices platform. Built on **Ocelot** (a .NET API gateway library), it acts as a reverse proxy that routes incoming HTTP requests to the appropriate downstream microservice based on URL path patterns. Beyond routing, the Gateway handles **JWT authentication validation**, **cross-origin resource sharing (CORS)**, **aggregate health checking** of all downstream services, and **unified Swagger UI** aggregation — meaning all six service APIs are browsable from one UI. Technically, it is a lightweight, infrastructure-layer service with no domain logic or database, existing solely to enforce the API contract boundary between clients and the internal service mesh.

---

## Overall Architecture & Design Decisions

### Architecture Pattern: API Gateway Pattern
- **Pattern used:** API Gateway (Facade + Reverse Proxy)
- **Why this pattern?** In a microservices architecture, having clients talk directly to each service creates several problems: clients must know all service URLs, every service must handle CORS and auth independently, and adding cross-cutting concerns (rate limiting, logging) requires changes in every service. The Gateway solves all of this in one place.
- **Why Ocelot?** Ocelot is a purpose-built, .NET-native API gateway library. Alternatives such as YARP (Yet Another Reverse Proxy) offer more flexibility but require more manual configuration. Ocelot's JSON-driven routing configuration makes it trivial to add new routes without code changes.
- **Communication style:** Synchronous HTTP reverse proxy only. No message brokering happens at the Gateway layer — that is handled within individual services via RabbitMQ.
- **How it fits:** Every UI/client call → hits `http://localhost:5000/gateway/...` → Gateway validates JWT → Ocelot rewrites the URL → forwards to downstream service port.

### Key Design Decision: JWT Validated at Gateway
Rather than each downstream service independently verifying JWTs, the **Gateway decodes and validates the JWT using the same secret key** as the IdentityService. Downstream services still have their own `AddAuthentication` setup as a defence-in-depth measure, but the Gateway is the primary enforcement wall. This is the standard enterprise pattern: centralise token validation, reduce latency from repeated cryptographic operations per service.

---

## Folder Structure

```
SmartShip.Gateway/
├── HealthChecks/
│   └── DownstreamServicesHealthCheck.cs   # Custom IHealthCheck probing all 6 services
├── Program.cs                              # Application composition root — ALL setup here
├── ocelot.json                             # Routing rules for Development environment
├── ocelot.DockerJenkins.json               # Routing rules for Docker/CI environment (different hosts)
├── appsettings.json                        # JWT keys, Serilog, ServiceHealthCheck URLs
├── appsettings.DockerJenkins.json          # Overrides for Docker (container hostnames instead of localhost)
├── Dockerfile                              # Multi-stage Docker build
├── SmartShip.Gateway.csproj               # NuGet references
```

**Why no `Controllers/`, `Services/`, `Domain/` folders?**
Because a Gateway has zero business logic. It is entirely configuration-driven. Any business logic here would be an architectural violation — the Gateway must remain thin.

---

## API Endpoints / Message Consumers

The Gateway exposes **no REST endpoints of its own** (except `GET /` probe and `GET /health`). All other endpoints are **proxy pass-throughs** defined in `ocelot.json`.

---

### `GET /` — Root Probe

**Purpose:** Liveness check that simply returns a string confirming the Gateway process is running. LoadBalancers or monitoring scripts can ping this cheaply.

**Response:** `"→ SmartShip Gateway Running"` (plain text)

---

### `GET /health` — Aggregate Health Check

**Purpose:** Returns a rich JSON report of the health status of all six downstream services. This is what an ops dashboard or the Docker healthcheck polls.

**Business logic (step-by-step):**
1. ASP.NET Core's `HealthCheckMiddleware` calls `DownstreamServicesHealthCheck.CheckHealthAsync()`.
2. The check reads all `ServiceHealthChecks` keys from `appsettings.json` (Identity, Shipment, Tracking, Admin, Payment, Notification).
3. For each service, it fires an HTTP GET to that service's `/health` endpoint in **parallel** using `Task.WhenAll()` (critical — we don't want serial polling adding latency).
4. For each response:
   - `200 OK` → status = `"Healthy"`
   - Non-2xx → status = `"Degraded"`
   - `HttpRequestException` (connection refused / DNS failure) → status = `"Unreachable"`
   - `TaskCanceledException` (3-second timeout) → status = `"Timeout"`
5. The Gateway rolls up into a single `HealthCheckResult`:
   - Any `Unreachable/Timeout` → `Unhealthy` (HTTP 503)
   - Any `Degraded` → `Degraded` (HTTP 200 with degraded status)
   - All healthy → `Healthy` (HTTP 200)

**Response example:**
```json
{
  "gateway": "SmartShip Gateway",
  "status": "Healthy",
  "timestamp": "13-Apr-2026 09:30:00 PM",
  "totalDurationMs": "47.82 ms",
  "services": {
    "Identity": { "status": "Healthy", "url": "http://localhost:5001/health", "statusCode": 200 },
    "Shipment": { "status": "Healthy", "url": "http://localhost:5002/health", "statusCode": 200 }
  },
  "summary": { "total": 6, "healthy": 6, "unhealthy": 0, "degraded": 0 }
}
```

**Why `Task.WhenAll()`?** Sequential polling of 6 services, each with a 3-second timeout, would make health checks take up to 18 seconds. Parallel polling caps total time at ~3 seconds in the worst case.

---

### Proxied Routes (ocelot.json)

All proxied routes follow a strict naming convention: `UpstreamPathTemplate` (what the client calls) maps to `DownstreamPathTemplate` (where Ocelot forwards to).

| Upstream Path (client calls) | Downstream (internal) | port | Auth Required |
|---|---|---|---|
| `/gateway/auth/{everything}` | `/api/auth/{everything}` | 5001 | ❌ No |
| `/gateway/admin/users/{everything}` | `/api/admin/users/{everything}` | 5001 | ✅ Bearer |
| `/gateway/shipments/{everything}` | `/api/shipments/{everything}` | 5002 | ✅ Bearer |
| `/gateway/admin/shipments/{everything}` | `/api/admin/shipments/{everything}` | 5002 | ✅ Bearer |
| `/gateway/tracking/{everything}` | `/api/tracking/{everything}` | 5003 | ✅ Bearer |
| `/gateway/admin/{everything}` | `/api/admin/{everything}` | 5004 | ✅ Bearer |
| `/gateway/payment/create-order` | `/api/payment/create-order` | 5005 | ✅ Bearer |
| `/gateway/payment/verify` | `/api/payment/verify` | 5005 | ✅ Bearer |
| `/gateway/payment/payment-status` | `/api/payment/payment-status` | 5005 | ✅ Bearer |
| `/gateway/payment/shipment/{shipmentId}` | `/api/payment/shipment/{shipmentId}` | 5005 | ✅ Bearer |
| `/gateway/notifications/{everything}` | `/api/notifications/{everything}` | 5006 | ✅ Bearer |

**Important design note:** `/gateway/auth/{everything}` is deliberately NOT protected. Why? Because login and signup requests cannot carry a Bearer token — the token doesn't exist yet! Any route not annotated with `AuthenticationOptions` in `ocelot.json` is open.

**Why {everything} wildcard?** Ocelot's `{everything}` placeholder captures the entire remaining path segment, including subpaths and query strings. This means a route like `/gateway/shipments/123?status=Booked` is correctly forwarded to `/api/shipments/123?status=Booked` without any code changes when new sub-routes are added.

**The `app.UseWhen` pattern:**
```csharp
app.UseWhen(
    ctx => ctx.Request.Path.StartsWithSegments("/gateway"),
    ocelotBranch => ocelotBranch.UseOcelot().Wait()
);
```
Only requests under `/gateway` are routed through Ocelot. The `/health` and `/` routes bypass Ocelot entirely and are handled by ASP.NET Core's own pipeline. This is a middleware branching pattern — elegant separation of concerns.

---

## Core Code Deep Dive (File-by-File)

### `Program.cs` — The Monolithic Composition Root

**What it does:** Configures the entire Gateway in ~192 lines. ASP.NET Core's "minimal hosting model" (introduced in .NET 6+) eliminates the need for a separate `Startup.cs`, collapsing DI registration, middleware pipeline, and `app.Run()` into one file.

**Key sections explained:**

**Bootstrap Logging (Lines 11–13):**
```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();
```
This creates a temporary Serilog logger before the DI container is built. If the app crashes during startup (e.g., missing JWT key), this logger ensures the crash reason is printed to console instead of being silently swallowed.

**Dual Ocelot Configuration Loading (Lines 21–24):**
```csharp
builder.Configuration
    .AddJsonFile("ocelot.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"ocelot.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);
```
`reloadOnChange: true` means if you update `ocelot.json` on disk, the Gateway hot-reloads routes **without a restart**. This is powerful for production: you can add new downstream service routes with zero downtime. The environment-specific file (`ocelot.DockerJenkins.json`) overrides `localhost` hostnames with Docker container names.

**JWT Guard (Lines 37–38):**
```csharp
if (string.IsNullOrWhiteSpace(jwtKey))
    throw new InvalidOperationException("JwtSettings:Key is missing.");
```
Fail-fast pattern. Instead of letting the app start and then silently failing to validate tokens, startup is immediately aborted if the secret key is absent. This prevents a subtle security hole where all tokens would be accepted if validation was misconfigured.

**`AddSwaggerForOcelot`:** Aggregates all downstream Swagger JSON specs into a single UI accessible at the Gateway. Each downstream service's `SwaggerKey` in `ocelot.json` maps to a `SwaggerEndPoints` entry pointing to that service's swagger doc URL. The Gateway fetches these and merges them.

---

### `HealthChecks/DownstreamServicesHealthCheck.cs`

**What it does:** Implements `IHealthCheck` — ASP.NET Core's standardized health check interface.

**Why `IHttpClientFactory` instead of `new HttpClient()`?**
`HttpClient` instances should never be `new`ed directly in a loop — socket exhaustion occurs when connections pile up in `TIME_WAIT` state. `IHttpClientFactory` manages a pool of `HttpMessageHandler` instances, reusing underlying TCP connections. The named client `"HealthCheckClient"` is configured with a 3-second timeout — just enough to determine if a service is alive, short enough not to block the health check endpoint.

**Concurrency with `Task.WhenAll()` (Line 95):**
All 6 HTTP pings fire simultaneously. The dictionary `results` is written from multiple tasks — this is safe because each task writes to a different key (service name), so there are no race conditions despite no explicit locking.

---

## Key Technologies & Libraries Used

| Library | Version | Why Used |
|---|---|---|
| **Ocelot** | Latest | .NET-native API gateway; JSON-driven routing, no code changes for new routes |
| **MMLib.SwaggerForOcelot** | Latest | Aggregates all downstream service Swagger docs into one UI |
| **Microsoft.AspNetCore.Authentication.JwtBearer** | Built-in | JWT validation middleware — parses and validates Bearer tokens |
| **Serilog** | Latest | Structured logging; log enrichment with `Application` and `Environment` properties |
| **Serilog.AspNetCore** | Latest | `UseSerilogRequestLogging()` — logs every proxied request with method, path, status, timing |
| **AspNetCore.HealthChecks** | Built-in | `IHealthCheck` interface — standardized health monitoring |

---

## Data Flow Examples

### Flow 1: Authenticated Customer Creates a Shipment

```
Client
  │
  ├─ POST /gateway/shipments
  │   Authorization: Bearer eyJhbGci...
  │
  ▼
Gateway (port 5000)
  ├─ 1. Serilog logs: "GATEWAY POST /gateway/shipments"
  ├─ 2. UseAuthentication() validates JWT signature + expiry against JwtSettings:Key
  ├─ 3. Route matched: /gateway/shipments → downstream port 5002
  ├─ 4. Ocelot rewrites request: POST /api/shipments
  └─ 5. Response proxied back to client
```

### Flow 2: Unauthenticated Login Attempt

```
Client
  ├─ POST /gateway/auth/login  (no Authorization header)
  ▼
Gateway
  ├─ 1. Route matched: /gateway/auth/{everything} — No AuthenticationOptions
  ├─ 2. Ocelot forwards → POST /api/auth/login on port 5001
  └─ 3. IdentityService responds with JWT token
```

### Flow 3: Health Check Poll

```
LoadBalancer / Monitoring
  ├─ GET /health
  ▼
Gateway
  ├─ DownstreamServicesHealthCheck fires
  ├─ Parallel HTTP GETs to all 6 service /health endpoints
  ├─ Results aggregated
  └─ Returns JSON report (HTTP 200 Healthy or 503 Unhealthy)
```

---

## Interview-Ready Insights

### Potential Interview Questions

1. **"Why use Ocelot instead of building your own reverse proxy?"**
   → Ocelot provides battle-tested reverse proxy features (load balancing, rate limiting, caching) with zero-code JSON config. Building a custom proxy would require reinventing all of this.

2. **"How does JWT get validated at the Gateway? Does the downstream service re-validate?"**
   → The Gateway validates the JWT signature against the shared HMAC-SHA256 key. Downstream services also have `AddAuthentication` registered, but since all internal traffic is already trusted (inside Docker network), re-validation is redundant. It exists as a defence-in-depth layer.

3. **"What happens if a downstream service goes down?"**
   → Ocelot returns an HTTP 502/503 to the client. The `/health` endpoint marks that service as `Unhealthy`. There is no built-in circuit breaker or retry logic configured — this is a **potential improvement** (see below).

4. **"Why `Task.WhenAll` in the health check?"**
   → Serial polling 6 services × 3-second timeout = up to 18 seconds. Parallel polling caps at 3 seconds. Health checks should be fast.

5. **"What is `reloadOnChange: true` in ocelot.json?"**
   → Ocelot reloads its route table from disk without restarting the process. This enables zero-downtime route changes in production.

### Potential Improvements / Scaling Concerns

- **No Circuit Breaker:** If `payment-service` is down and a client spam-calls the payment endpoint, the Gateway blindly keeps forwarding. **Fix:** Use Ocelot's built-in Polly integration or add `Ocelot.Provider.Polly` for circuit breakers and retries.
- **No Rate Limiting:** Any IP can hammer the Gateway. Ocelot supports rate limiting per route — not configured here.
- **HTTP Only:** The `ocelot.json` uses `"DownstreamScheme": "http"`. In production, `https` with mutual TLS between Gateway and services would be more secure.
- **Single Gateway Instance:** If the Gateway goes down, everything goes down. In production, you'd run multiple Gateway replicas behind a load balancer (e.g., Nginx/HAProxy), making the Gateway stateless (which it already is — no DB, no sessions).
- **Shared JWT Secret:** The Gateway and IdentityService share the same HMAC key in `appsettings.json` (hardcoded). A production system should use **Azure Key Vault** or **HashiCorp Vault** to inject secrets.

### Trade-offs Made

| Decision | Trade-off |
|---|---|
| Ocelot over YARP | Simpler JSON config; YARP offers more programmatic control |
| HMAC-SHA256 JWT | Fast symmetric validation; RSA asymmetric would allow services to validate tokens without sharing secrets |
| No circuit breaker | Simpler; risk of cascading failures under load |
| Auth validated at Gateway level | One enforcement point; downstream services trust all traffic from Gateway |
