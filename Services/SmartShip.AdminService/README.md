# SmartShip.AdminService — Administration & Operations Service

## Overview

The **AdminService** is the back-office control plane for the SmartShip platform. It provides administrative capabilities that cross the domains of other services: aggregated **dashboard metrics** (total shipments, active shipments, delivered today, exceptions, total customers), **logistics hub management** (CRUD for physical distribution centers), and **report generation** (Operational, Performance, SLA, Delivery reports as on-demand snapshots). Critically, the AdminService is **not a typical admin facade that calls other services** — it maintains its own denormalized metrics by consuming events from the message bus and auto-incrementing/decrementing counters. This event-driven counter approach makes dashboard queries instantaneous (no cross-service aggregation on-demand). It runs on **port 5004** and is entirely restricted to users with the `ADMIN` role.

---

## Overall Architecture & Design Decisions

### Architecture Pattern: Layered Architecture — Event-Driven CQRS-lite

```
API Layer           → AdminController (REST: dashboard, hubs, reports)
Core Layer          → IAdminService + AdminService (business logic, role guard)
Domain Layer        → DashboardMetrics, Hub, Report entities
Infrastructure Layer → AdminDbContext + Repositories + UnitOfWork
                       Messaging/Consumers (6 event consumers updating counters)
```

**What makes this "CQRS-lite"?**  
Reads (dashboard queries) are served from denormalized, pre-computed `DashboardMetrics`. Writes happen via event consumers that update these metrics. This is a simplified Command Query Responsibility Segregation where:
- **Commands:** Business events from other services (ShipmentCreated, UserCreated, etc.) update the write model (DashboardMetrics).
- **Queries:** REST calls read the pre-computed read model.

**Alternative considered:** The dashboard could query ShipmentService, IdentityService, and PaymentService via HTTP on every load. This was rejected because:
1. Each dashboard load would trigger 3–5 synchronous HTTP calls.
2. Any single downstream service going down breaks the dashboard.
3. High-traffic dashboards (refreshed every 5 seconds) would create enormous cross-service load.

**Communication:**
- **Inbound REST:** Dashboard, hubs, reports (all ADMIN role).
- **Inbound Events:** Consumes 6 event types to maintain real-time counters.
- **Outbound:** Nothing — AdminService is a consumer and read-model server only.

---

## Folder Structure

```
SmartShip.AdminService/
├── API/
│   ├── Controllers/
│   │   └── AdminController.cs            # 10+ endpoints for dashboard, hubs, reports
│   └── Middleware/
│       └── ExceptionMiddleware.cs
├── Core/
│   ├── DTOs/
│   │   ├── DashboardDTOs.cs               # DashboardMetricsDto
│   │   ├── HubDTOs.cs                     # CreateHubRequest, UpdateHubRequest, HubDto, HubPagedRequest
│   │   └── ReportDTOs.cs                  # ReportRequest, ReportDto, ReportPagedRequest
│   ├── Interfaces/
│   │   ├── Repositories/IHubRepository.cs, IDashboardMetricsRepository.cs, IReportRepository.cs
│   │   ├── Services/IAdminService.cs
│   │   └── Persistence/IUnitOfWork.cs
│   └── Services/
│       └── AdminService.cs                # Role guard + dashboard + hub CRUD + report generation
│   └── Validators/
│       └── AdminValidators.cs             # FluentValidation for hub and report requests
├── Domain/
│   ├── Entities/
│   │   ├── DashboardMetrics.cs            # Singleton metrics row: totals, actives, delivered, exceptions
│   │   ├── Hub.cs                         # Physical distribution hub entity
│   │   └── Report.cs                      # Generated report with DataJson snapshot
│   └── Enums/
│       └── ReportType.cs                  # Operational, Performance, SLA, Delivery
├── Infrastructure/
│   ├── Data/AdminDbContext.cs
│   ├── Messaging/Consumers/
│   │   ├── ShipmentCreatedConsumer.cs     # Increments TotalShipments + ActiveShipments
│   │   ├── ShipmentDeliveredConsumer.cs   # Decrements ActiveShipments, increments DeliveredToday
│   │   ├── ShipmentCancelledConsumer.cs   # Decrements ActiveShipments
│   │   ├── PaymentFailedConsumer.cs       # Increments Exceptions
│   │   ├── UserCreatedConsumer.cs         # Increments TotalCustomers (if CUSTOMER role)
│   │   └── UserDeletedConsumer.cs         # Decrements TotalCustomers (if CUSTOMER role)
│   ├── Persistence/UnitOfWork.cs
│   └── Repositories/
│       ├── DashboardMetricsRepository.cs  # GetFirstAsync (singleton row), AddAsync, UpdateAsync
│       ├── HubRepository.cs               # Full paginated CRUD with sort + filter + search
│       └── ReportRepository.cs            # Add + paged list with type/date filters
├── Program.cs
└── appsettings.json
```

**Why `DashboardMetrics` is a singleton row:**  
`DashboardMetrics` stores system-wide counters. There is only ONE row in this table (`GetFirstAsync()` always fetches the same row). This is the **Singleton Row pattern** — conceptually equivalent to a settings/config table. Counters are updated atomically by event consumers.

---

## API Endpoints / Message Consumers

### `GET /api/admin/dashboard` — Dashboard Metrics

**Auth:** Bearer JWT (`ADMIN` role required)

**Purpose:** Returns the pre-computed operational metrics for the admin dashboard.

**Business logic:**
1. `EnsureAdminAccess()` — internal helper that checks `ClaimTypes.Role == "ADMIN"`, throws `UnauthorizedAccessException` if not admin.
2. `DashboardMetricsRepository.GetFirstAsync()` — fetches the singleton row.
3. If no row exists (fresh installation) → returns default empty metrics.
4. Return `DashboardMetricsDto`.

**Why not check `[Authorize(Roles="ADMIN")]` on the controller?**  
The service layer has an additional `EnsureAdminAccess()` guard. This is **defence in depth** — if the controller attribute is accidentally removed, the service layer still enforces authorization. This pattern is common in enterprise systems where business logic should not blindly trust the caller's auth status.

**Response:**
```json
{
  "totalShipments": 450,
  "activeShipments": 87,
  "deliveredToday": 23,
  "exceptions": 5,
  "totalCustomers": 312,
  "lastUpdatedAt": "13-Apr-2026 09:00 PM"
}
```

---

### `GET /api/admin/hubs` — List Hubs (Paginated)

**Auth:** ADMIN

**Request:** `GET /api/admin/hubs?page=1&pageSize=10&isActive=true&city=Mumbai&sortBy=name&sortOrder=asc`

**Business logic:**
1. `HubRepository.GetPagedAsync(req)` — dynamic query with:
   - Filter: `IsActive`, `City` contains, `State` contains.
   - Search: `Name`, `City`, or `State` contains search term.
   - Sort: `name`, `city`, or default `createdAt`, with `asc/desc` order.

**Why server-side sort in the repository?** The switch expression:
```csharp
query = req.SortBy?.ToLower() switch {
    "name" => req.SortOrder == "asc" ? query.OrderBy(h => h.Name) : query.OrderByDescending(h => h.Name),
    "city" => req.SortOrder == "asc" ? query.OrderBy(h => h.City) : query.OrderByDescending(h => h.City),
    _ => req.SortOrder == "asc" ? query.OrderBy(h => h.CreatedAt) : query.OrderByDescending(h => h.CreatedAt)
};
```
This is a **sort strategy** pattern implemented via C# 8 switch expressions. Each case generates a different SQL `ORDER BY` clause. Client-side sorting after `ToListAsync()` would load ALL rows into memory first — inefficient for 1000+ hubs.

---

### `POST /api/admin/hubs` — Create Hub

**Auth:** ADMIN

**Request:**
```json
{
  "name": "Mumbai North Distribution Hub",
  "city": "Mumbai",
  "state": "Maharashtra",
  "country": "India",
  "contactPhone": "9876543210"
}
```

**Business logic:**
1. FluentValidation: name 3–100 chars, city/state letters only, 10-digit phone.
2. Create `Hub` entity with `IsActive = true`.
3. Persist via UoW.
4. Return `HubDto`.

---

### `PUT /api/admin/hubs/{id}` — Update Hub

**Business logic:**
1. Fetch hub by ID → 404 if not found.
2. Update: name, city, state, country, phone, `IsActive` (can decommission hubs by setting `IsActive = false`).
3. SaveChanges.

**Why `PUT` (full replace) instead of `PATCH` (partial update)?**  
Hub updates always replace all fields — the UI always sends the full hub form. PATCH would be appropriate if only a subset of fields could be updated independently.

---

### `DELETE /api/admin/hubs/{id}` — Delete Hub

**Business logic:** Hard delete. No event published — hub deletion is a purely administrative operation that doesn't affect other services.

---

### `POST /api/admin/reports/generate` — Generate Report

**Auth:** ADMIN

**Request:**
```json
{
  "reportType": "Operational",
  "fromDate": "2026-04-01T00:00:00",
  "toDate": "2026-04-13T23:59:59"
}
```

**Business logic (step-by-step):**
1. FluentValidation: `reportType` must be `"Operational"`, `"Performance"`, `"SLA"`, or `"Delivery"`. `fromDate < toDate`.
2. Fetch current `DashboardMetrics`.
3. Build a report data snapshot:
   ```csharp
   var data = new {
       TotalShipments = metrics.TotalShipments,
       Delivered = metrics.TotalShipments - metrics.ActiveShipments,
       Exceptions = metrics.Exceptions,
       ActiveShipments = metrics.ActiveShipments,
       GeneratedFrom = req.FromDate,
       GeneratedTo = req.ToDate
   };
   ```
4. Serialize to JSON: `DataJson = JsonSerializer.Serialize(data)`.
5. Generate title: `"{ReportType} Report (01/04/2026 - 13/04/2026)"`.
6. Record `GeneratedBy = GetCurrentUserName()` from JWT claim.
7. Persist `Report` entity.
8. Return `ReportDto` with the data object deserialized for the response.

**Design note — `DataJson` column:**  
The report data is stored as JSON in a `string` column rather than strongly-typed columns. This is the **schema-less report storage pattern**. Report types may have different data shapes in the future. Storing as JSON avoids adding columns whenever a new report type is added. The trade-off: JSON is not queryable with standard SQL predicates.

**Interview insight — why store a snapshot?**  
If the report only stored the date range and we re-queried metrics on every load, the report would show *current* data, not data from when the report was generated. A snapshot at generation time is the correct approach for auditable business reports.

---

### `GET /api/admin/reports` — List Reports (Paginated)

**Auth:** ADMIN

**Request:** `GET /api/admin/reports?reportType=Operational&fromDate=2026-04-01&toDate=2026-04-30&page=1&pageSize=10`

**Business logic:**
1. `ReportRepository.GetPagedAsync(req)` with filter by `ReportType` enum, date range on `GeneratedAt`.
2. Default sort: `OrderByDescending(r => r.GeneratedAt)` — newest reports first.

---

## Message Consumers (Event-Driven Metrics Updates)

These 6 consumers maintain the `DashboardMetrics` singleton row in near-real-time.

### `ShipmentCreatedConsumer`

**Event:** `ShipmentCreatedEvent`  
```csharp
metrics.TotalShipments++;
metrics.ActiveShipments++;
metrics.LastUpdatedAt = DateTime.Now;
await _db.SaveChangesAsync();
```

**Why increment and not recalculate?** Recounting all shipments via `SELECT COUNT(*)` from another service's database is impossible (no shared DB) or very slow. Event-driven counter increment is instant and accurate as long as events are reliably delivered.

### `ShipmentDeliveredConsumer`

**Event:** `ShipmentDeliveredEvent`  
```csharp
metrics.ActiveShipments = Math.Max(0, metrics.ActiveShipments - 1);
metrics.DeliveredToday++;
```

**Why `Math.Max(0, ...)`?** If an event is consumed twice (at-least-once delivery), a naive decrement would go negative. `Math.Max(0, x - 1)` is a **safe decrement** — idempotent for duplicate events (second decrement: `Max(0, 0-1) = 0`). This is a lightweight idempotency guard.

### `ShipmentCancelledConsumer`

**Event:** `ShipmentCancelledEvent`  
```csharp
metrics.ActiveShipments = Math.Max(0, metrics.ActiveShipments - 1);
```

### `PaymentFailedConsumer`

**Event:** `PaymentFailedEvent`  
```csharp
metrics.Exceptions++;
```

**Why is PaymentFailed an "Exception"?** In logistics operations, an exception is any event that requires human attention — failed payments, customs holds, delivery failures. The dashboard `Exceptions` counter alerts the ops team that something needs intervention.

### `UserCreatedConsumer`

**Event:** `UserCreatedEvent`  
```csharp
if (msg.Role == "CUSTOMER")  // Don't count ADMIN users in customer total
    metrics.TotalCustomers++;
```

**The `Role == "CUSTOMER"` guard:** The system has admin users too. The `TotalCustomers` metric should reflect paying customers, not internal users. This conditional is a business rule baked into the counter update.

### `UserDeletedConsumer`

**Event:** `UserDeletedEvent`  
```csharp
if (!msg.Email.Contains("admin") && msg.Role == "CUSTOMER")
    metrics.TotalCustomers = Math.Max(0, metrics.TotalCustomers - 1);
```

Both the email `Contains("admin")` check AND the `Role == "CUSTOMER"` check are applied. Double-guard because:
- A customer with "admin" in their email should still be counted as a customer.
- Safe decrement for idempotency protection.

---

## Core Code Deep Dive

### `Core/Services/AdminService.cs` — Role Guard Pattern

```csharp
private void EnsureAdminAccess()
{
    var role = _httpContextAccessor.HttpContext?
        .User.FindFirst(ClaimTypes.Role)?.Value;
    if (role != "ADMIN")
        throw new UnauthorizedAccessException("Admin access required.");
}
```

Called at the start of every service method. This is the **Fail Fast** principle applied to authorization in the service layer. Alternative: use `[Authorize(Roles = "ADMIN")]` on every controller action — cleaner, but this service-layer guard provides defense in depth.

### `Infrastructure/Repositories/HubRepository.cs` — Combined Filter + Sort + Paginate

```csharp
public async Task<PagedResponse<Hub>> GetPagedAsync(HubPagedRequest req)
{
    var query = _context.Hubs.AsQueryable();

    // Dynamic filters
    if (req.IsActive.HasValue) query = query.Where(h => h.IsActive == req.IsActive.Value);
    if (!string.IsNullOrWhiteSpace(req.City)) query = query.Where(h => h.City.Contains(req.City));
    if (!string.IsNullOrWhiteSpace(req.Search)) query = query.Where(h =>
        h.Name.Contains(req.Search) || h.City.Contains(req.Search) || h.State.Contains(req.Search));

    // Sort strategy
    query = req.SortBy?.ToLower() switch { ... };

    // Two queries: count (no Skip/Take) + paginated data
    var totalCount = await query.CountAsync();
    var items = await query.Skip((req.Page-1)*req.PageSize).Take(req.PageSize).ToListAsync();

    return new PagedResponse<Hub> { Data = items, TotalCount = totalCount, ... };
}
```

This is the gold standard pagination implementation:
1. All filters applied before `CountAsync()` → count reflects filtered total.
2. `CountAsync()` before `Skip/Take` → needed for frontend pagination controls.
3. Deferred IQueryable — all predicates compose into a single SQL query.

### `Domain/Entities/Report.cs`

```csharp
public string DataJson { get; set; } = string.Empty;
```

The `DataJson` field stores arbitrary report data serialized as JSON. When returned in `ReportDto`, it's deserialized back to `object`:

```csharp
return new ReportDto(
    r.Id, r.Title, r.ReportType.ToString(), r.FromDate, r.ToDate, r.GeneratedAt,
    r.DataJson  // Sent as raw JSON string, frontend deserializes
);
```

**Trade-off:** `DataJson` as `string` vs. EF Core's `JSON column type` (available in EF 7+). EF Core JSON columns allow querying inside the JSON with LINQ — not implemented here but would be the upgrade path.

---

## Key Technologies & Libraries Used

| Technology | Why Used |
|---|---|
| **MassTransit + RabbitMQ** | 6 event consumers maintaining real-time counter updates |
| **Entity Framework Core** | ORM; three aggregate roots in AdminDbContext |
| **FluentValidation** | Hub and report request validation |
| **System.Text.Json** | Report data serialization to/from JSON column |
| **IHttpContextAccessor** | Access JWT claims inside service layer (for admin role guard + `GetCurrentUserName`) |
| **Serilog** | Structured logging; every consumer and service method logs with context |

---

## Data Flow Examples

### Flow: Real-time Dashboard Counter Update

```
ShipmentService publishes ShipmentCreatedEvent
  ↓ RabbitMQ → AdminService/ShipmentCreatedConsumer
  ├── Fetches DashboardMetrics singleton row
  ├── TotalShipments++, ActiveShipments++
  └── SaveChangesAsync()

Admin calls GET /api/admin/dashboard
  └── Returns { TotalShipments: 451, ActiveShipments: 88, ... }
  // Instantly served — no cross-service query
```

### Flow: Report Generation

```
Admin: POST /api/admin/reports/generate
  { reportType: "Operational", fromDate: "2026-04-01", toDate: "2026-04-13" }

AdminService:
  ├── EnsureAdminAccess() — verify ADMIN role
  ├── Fetch DashboardMetrics
  ├── Snapshot: { TotalShipments: 450, Delivered: 363, Exceptions: 5, ... }
  ├── DataJson = JsonSerializer.Serialize(snapshot)
  ├── Report saved: "Operational Report (01/04/2026 - 13/04/2026)"
  └── Return ReportDto with snapshot data

// Report is now immutable — re-fetching it always shows April 13 state
// even if metrics change later
```

---

## Interview-Ready Insights

### Potential Interview Questions

1. **"Why does AdminService maintain its own metrics instead of querying other services?"**
   → Avoids cascade failures (one service down = broken dashboard), eliminates latency of cross-service HTTP calls, enables instant read access. The event-driven counter pattern is a classic materialized view strategy.

2. **"Why use `Math.Max(0, counter - 1)` instead of `counter--`?"**
   → Guard against duplicate event consumption (at-least-once delivery). Also prevents negative values if an event fires out of order (e.g., Delivered before Created was processed in the admin service).

3. **"Why does `UserDeletedConsumer` have a `!msg.Email.Contains("admin")` check?"**
   → Belt-and-suspenders check to avoid decrementing `TotalCustomers` for admin accounts. The `Role == "CUSTOMER"` check should be sufficient, but the email check adds another guard. In practice, this is over-engineering — one check would suffice.

4. **"What is the `DataJson` column in the Report entity?"**
   → Schema-flexible JSON storage for report data. Different report types may have different shapes. Serializing to JSON allows addding new report types without schema migrations. Downside: no SQL queries inside the JSON (without EF Core JSON columns).

5. **"How would you make the dashboard truly real-time (e.g., live WebSocket updates)?"**
   → Add a SignalR hub in AdminService. Event consumers would call `hubContext.Clients.All.SendAsync("MetricsUpdated", metrics)` after each counter update. The admin dashboard frontend connects via WebSocket and receives live pushes.

### Potential Improvements

- **Race Conditions:** Two `ShipmentCreatedConsumer` instances running in parallel could both read `TotalShipments = 450`, both increment to 451, and both save — resulting in count 451 instead of 452. Fix: use SQL `UPDATE DashboardMetrics SET TotalShipments = TotalShipments + 1` (atomic DB increment) instead of read-modify-write.
- **SignalR Integration:** Push metric updates to admin dashboards in real-time instead of requiring page refresh.
- **Report Scheduling:** Allow admins to schedule automated daily/weekly reports instead of only on-demand.
- **Separate Read/Write DB:** Use a Redis in-memory store for counters (extreme performance) and mirror to SQL for persistence.

### Trade-offs Made

| Decision | Trade-off |
|---|---|
| Event-driven counters | Eventual consistency (tiny delay) vs. perfect consistency via direct queries |
| DataJson report storage | Schema flexibility vs. query-ability |
| Singleton DashboardMetrics row | Simple; race conditions without atomic SQL updates |
| Service-layer role guard | Defence in depth; redundant with controller attribute |
