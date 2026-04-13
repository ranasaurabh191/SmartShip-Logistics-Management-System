# SmartShip.TrackingService — Shipment Tracking & Documentation Service

## Overview

The **TrackingService** is the real-time visibility layer of the SmartShip platform. It has two distinct responsibilities: (1) maintaining a **chronological timeline of tracking events** for every shipment (pickup attempts, location updates, status changes, delays) and (2) managing **shipment-related documents** (invoices, shipping labels) and **delivery proof** (recipient signature + photo captures). It is a **pure consumer service** — it never initiates API calls to other services. All data arrives either via RabbitMQ events (from Shipment and Payment services) or via direct REST calls from admins/customers. It runs on **port 5003** and persists three independent aggregate types: `TrackingEvent`, `Document`, and `DeliveryProof`.

---

## Overall Architecture & Design Decisions

### Architecture Pattern: Layered Architecture — Event-Driven Consumer

```
API Layer           → TrackingController (REST: query + add events manually)
Core Layer          → ITrackingService + TrackingService (business logic)
                       DTOs + Interfaces
Domain Layer        → TrackingEvent, Document, DeliveryProof entities + DocumentType enum
Infrastructure Layer → TrackingDbContext + Repositories (UoW)
                       Messaging/Consumers (5 MassTransit consumers)
```

**Why is TrackingService a "pure consumer"?**  
TrackingService reacts to what happens elsewhere — when a shipment is created, payment is made, or status changes, TrackingService records those facts. It never needs to *initiate* actions. This one-directional event flow keeps TrackingService decoupled and simple: it is essentially a **materialized view service** that projects domain events into queryable tracking timelines.

**Communication:**
- **Inbound REST:** Query tracking history, upload documents, add delivery proof (admin).
- **Inbound Events:** Consumes 7 different RabbitMQ events to create tracking timeline entries.
- **Outbound:** Nothing — pure consumer.

---

## Folder Structure

```
SmartShip.TrackingService/
├── API/
│   ├── Controllers/
│   │   └── TrackingController.cs          # REST endpoints for tracking queries + doc upload
│   └── Middleware/
│       └── ExceptionMiddleware.cs
├── Core/
│   ├── DTOs/
│   │   ├── TrackingEventDTOs.cs            # TrackingEventDto, TrackingEventPagedRequest
│   │   ├── DocumentDTOs.cs                 # DocumentDto, DocumentPagedRequest, UploadDocumentRequest
│   │   └── DeliveryProofDTOs.cs            # DeliveryProofDto, AddDeliveryProofRequest
│   ├── Interfaces/
│   │   ├── Repositories/                   # ITrackingEventRepository, IDocumentRepository, IDeliveryProofRepository
│   │   ├── Services/ITrackingService.cs
│   │   └── Persistence/IUnitOfWork.cs
│   └── Services/
│       └── TrackingService.cs              # Business logic: add events, upload docs, delivery proof
├── Domain/
│   ├── Entities/
│   │   ├── TrackingEvent.cs                # Timeline entry: shipmentId, trackingNumber, status, location, time
│   │   ├── Document.cs                     # Uploaded file metadata: path, type, size, uploader
│   │   └── DeliveryProof.cs                # Proof of delivery: recipient name, signature, photo
│   └── Enums/
│       └── DocumentType.cs                # Invoice, ShippingLabel, Other
├── Infrastructure/
│   ├── Data/TrackingDbContext.cs
│   ├── Messaging/Consumers/
│   │   ├── ShipmentCreatedConsumer.cs      # Creates "Shipment Created" tracking event
│   │   ├── ShipmentStatusUpdatedConsumer.cs # Creates status-change tracking events
│   │   ├── ShipmentDeliveredConsumer.cs    # Creates "Delivered" tracking event
│   │   ├── PaymentCreatedConsumer.cs       # Creates "Payment Initiated" event
│   │   ├── PaymentCompletedConsumer.cs     # Creates "Payment Verified" event
│   │   ├── PaymentFailedConsumer.cs        # Creates "Payment Failed" event
│   │   └── PaymentRefundedConsumer.cs      # Creates "Payment Refunded" event
│   ├── Persistence/UnitOfWork.cs
│   ├── Repositories/
│   │   ├── TrackingEventRepository.cs      # Paginated, filterable event history
│   │   ├── DocumentRepository.cs           # Documents by shipment, paginated + type filter
│   │   └── DeliveryProofRepository.cs      # By shipmentId or trackingNumber
│   └── Uploads/                            # Local filesystem storage for delivery proof images
├── Program.cs
└── appsettings.json
```

**Why is `Uploads/` in Infrastructure?**  
File storage is an infrastructure concern — it's a side effect of a business operation, not a domain concept. In production, this would be replaced by an Azure Blob Storage or AWS S3 client living in the Infrastructure layer.

---

## API Endpoints / Message Consumers

### `GET /api/tracking/{trackingNumber}` — Get Tracking Timeline

**Auth:** Bearer JWT (any role)

**Purpose:** Returns the full chronological tracking history for a shipment. This is what a customer sees on the "Track My Package" page.

**Request:** `GET /api/tracking/SS20260413154230001?page=1&pageSize=20`

**Business logic:**
1. Extract `TrackingNumber` from URL path.
2. `TrackingEventRepository.GetByTrackingNumberPagedAsync()` queries with optional filters:
   - `?status=InTransit` — filter by status keyword.
   - `?fromDate=2026-04-01&toDate=2026-04-15` — date range.
   - `?search=Delhi` — full-text search on location or description.
3. Default sort: `OrderByDescending(t => t.EventTime)` — newest event first.

**Response:**
```json
{
  "data": [
    {
      "id": 5,
      "trackingNumber": "SS20260413154230001",
      "status": "InTransit",
      "location": "Delhi Distribution Hub",
      "description": "Package arrived at Delhi hub",
      "eventTime": "13-Apr-2026 05:00 PM",
      "updatedBy": "System"
    }
  ],
  "totalCount": 5,
  "page": 1,
  "pageSize": 20
}
```

---

### `POST /api/tracking/events` — Manually Add Tracking Event (Admin)

**Auth:** Bearer JWT (ADMIN role)

**Purpose:** Allows operations staff to manually insert tracking events (e.g., "Customs hold in Mumbai airport", "Delivery attempted, no one home").

**Request:**
```json
{
  "shipmentId": 42,
  "trackingNumber": "SS20260413154230001",
  "status": "Delayed",
  "location": "Mumbai Airport Customs",
  "description": "Package held for customs inspection",
  "updatedBy": "ops_team"
}
```

**Business logic:**
1. **Deduplication check:** `GetRecentDuplicateAsync(trackingNumber, status, location, sinceTime)` — checks if an identical event (same status + location) was added in the last 5 minutes. Rejects if duplicate found.
2. Create `TrackingEvent` entity with `EventTime = DateTime.Now`.
3. Persist via UoW.

**Why deduplication?** Automated systems or event consumers might fire duplicate events in rapid succession (e.g., RabbitMQ at-least-once delivery). The 5-minute deduplication window prevents cluttered tracking timelines.

---

### `POST /api/tracking/delivery-proof` — Submit Delivery Proof (multipart/form-data)

**Auth:** Bearer JWT (ADMIN role — typically called by delivery agent's app)

**Purpose:** After delivering a package, the delivery agent submits a photo of the package and a digital signature image as proof of delivery.

**Request (multipart/form-data):**
- `shipmentId`: int
- `trackingNumber`: string
- `receiverName`: string (name of person who received)
- `deliveredBy`: string (agent's name/ID)
- `notes`: string
- `signatureImage`: file (image file)
- `photo`: file (image file)

**Business logic:**
1. Check `DeliveryProofRepository.GetByTrackingNumberAsync()` — reject if proof already submitted (idempotency guard).
2. Save files to `Infrastructure/Uploads/` directory with UUID-prefixed filenames: `{Guid}_{originalFilename}`.
3. Store `DeliveryProof` entity with file paths (NOT base64 — files stay on disk, paths in DB).
4. Return `DeliveryProofDto` with paths.

**Why store file paths, not base64 in DB?** Base64 encoding increases file size by ~33% and makes the DB row enormous for images. Storing paths keeps the DB lean and allows serving files via a static file server or CDN.

**Filename UUID prefix:** Prevents filename collisions when two deliveries upload a file named `signature.png`.

---

### `POST /api/tracking/documents` — Upload Shipment Document

**Auth:** Bearer JWT (any authenticated user)

**Purpose:** Attach documents (invoices, shipping labels, etc.) to a shipment for later retrieval.

**Request (multipart/form-data):**
- `shipmentId`: int
- `trackingNumber`: string
- `documentType`: `"Invoice"` | `"ShippingLabel"` | `"Other"`
- `file`: file

**Business logic:**
1. Check for duplicate: `GetByShipmentIdAndFileNameAsync(shipmentId, fileName)` — reject if same filename already uploaded for this shipment.
2. UUID-prefix the filename to prevent collisions.
3. `Directory.CreateDirectory(uploadPath)` — creates the `Uploads` folder if it doesn't exist (idempotent).
4. Stream file to disk: `file.CopyToAsync(stream)` — doesn't buffer entire file in memory.
5. Persist `Document` entity with `FilePath`, `FileSizeBytes`, `DocumentType`, `UploadedByUserId`.

**Why stream with `CopyToAsync`?** For large files (e.g., 10MB invoice PDFs), `file.CopyToAsync()` writes in chunks without loading the entire file into memory. `IFormFile.OpenReadStream()` would hold everything in RAM.

---

### `GET /api/tracking/documents/{shipmentId}` — List Documents

**Auth:** Bearer JWT

**Request:** `GET /api/tracking/documents/42?documentType=Invoice&page=1&pageSize=10`

**Business logic:** Paginated query by `shipmentId`, optional filter by `DocumentType` enum, search by `FileName`.

---

### `GET /api/tracking/delivery-proof/{trackingNumber}` — Get Delivery Proof

**Auth:** Bearer JWT

Returns the `DeliveryProof` record (receiver name, signature path, photo path, delivery time, agent name).

---

## Message Consumers (Event-Driven Tracking Creation)

These are the backbone of TrackingService — when business events happen elsewhere, tracking records are automatically created.

### `ShipmentCreatedConsumer`

**Event:** `ShipmentCreatedEvent`  
**Creates tracking entry:**  
```
Status: "Created"
Location: "{senderCity}"
Description: "Shipment created. Tracking number assigned: {trackingNumber}"
```

### `ShipmentStatusUpdatedConsumer`

**Event:** `ShipmentStatusUpdatedEvent`  
**Creates tracking entry:**
```
Status: "{newStatus}"
Location: "{location}"
Description: "Status changed from {oldStatus} to {newStatus}"
```
This consumer is called for ALL status transitions (Booked, PickedUp, InTransit, etc.), meaning every admin status update automatically creates a tracking event.

### `ShipmentDeliveredConsumer`

**Event:** `ShipmentDeliveredEvent`  
**Creates tracking entry:**
```
Status: "Delivered"
Location: "{location}"
Description: "Shipment delivered successfully at {deliveredAt}"
```

### `PaymentCreatedConsumer`

**Event:** `PaymentCreatedEvent`  
**Creates tracking entry:**
```
Status: "PaymentInitiated"
Description: "Payment of ₹{amount} initiated via {paymentMethod}"
```

**Why track payment events in the tracking timeline?**  
It gives customers a complete end-to-end view: "Your package was created → payment initiated → payment verified → picked up → in transit → delivered." This is user experience gold — the user doesn't need to check a separate payment status page.

### `PaymentCompletedConsumer`

**Event:** `PaymentCompletedEvent`  
```
Status: "PaymentVerified"
Description: "Payment verified. Shipment confirmed."
```

### `PaymentFailedConsumer`

**Event:** `PaymentFailedEvent`  
```
Status: "PaymentFailed"
Description: "Payment failed: {reason}"
```

### `PaymentRefundedConsumer`

**Event:** `PaymentRefundedEvent`  
```
Status: "Refunded"
Description: "Refund of ₹{amount} processed."
```

**Total consumer count:** 7 consumers covering the full shipment + payment event surface area.

---

## Core Code Deep Dive

### `Core/Services/TrackingService.cs` — Deduplication Logic

```csharp
var sinceTime = DateTime.Now.AddMinutes(-5);
var existing = await _eventRepository.GetRecentDuplicateAsync(
    trackingNumber, status, location, sinceTime);
if (existing != null)
    throw new InvalidOperationException($"Duplicate event: status '{status}' at '{location}'...");
```

The deduplication window is 5 minutes. This handles the most common scenario (event consumer fires twice due to at-least-once delivery) while still allowing the same status at the same location to be recorded again after the window (e.g., a package really is stuck at Delhi Hub for multiple hours — each hourly update should be recorded).

### `Infrastructure/Repositories/TrackingEventRepository.cs` — Flexible Query

```csharp
public async Task<PagedResponse<TrackingEvent>> GetByTrackingNumberPagedAsync(
    string trackingNumber, TrackingEventPagedRequest req)
{
    var query = _context.TrackingEvents
        .Where(t => t.TrackingNumber == trackingNumber)
        .AsQueryable();

    if (!string.IsNullOrEmpty(req.Status))
        query = query.Where(t => t.Status.Contains(req.Status));

    if (req.FromDate.HasValue)
        query = query.Where(t => t.EventTime >= req.FromDate.Value);

    // Note: Contains() → SQL LIKE '%value%' (substring match)
    // This handles "InTrans" matching "InTransit"
    ...
}
```

**`Contains()` vs `==` for status filter:** `Contains()` generates SQL `LIKE '%status%'`, enabling partial matching. A customer searching for `"Transit"` would find `"InTransit"` and `"OutForDelivery"` might not match. Consider this a trade-off: more flexible search vs. unpredictable results.

### `Infrastructure/Repositories/DocumentRepository.cs` — Enum Parsing

```csharp
if (!string.IsNullOrEmpty(req.DocumentType) &&
    Enum.TryParse<DocumentType>(req.DocumentType, true, out var dt))
{
    query = query.Where(d => d.DocumentType == dt);
}
```

**Why `TryParse` with `ignoreCase: true`?** Client might send `"invoice"` or `"INVOICE"`. `TryParse` with case-insensitivity handles all variations gracefully. If it fails to parse, the filter is simply skipped — no exception thrown.

### `Domain/Entities/DeliveryProof.cs`

```csharp
public string? SignatureImagePath { get; set; }   // nullable — can submit proof without signature
public string? PhotoPath { get; set; }           // nullable — can submit with photo only
```

Both are nullable because real-world delivery might not always collect both (e.g., contactless delivery during COVID policies — photo only, no signature).

---

## Key Technologies & Libraries Used

| Technology | Why Used |
|---|---|
| **MassTransit + RabbitMQ** | Receives 7 event types from Shipment and Payment services |
| **Entity Framework Core** | ORM; three separate aggregate roots in one DbContext |
| **IFormFile / multipart/form-data** | ASP.NET Core file upload handling; streams to filesystem |
| **FluentValidation** | Request validation for manually added tracking events |
| **Serilog** | Logs every consumer event received for traceability |

---

## Data Flow Examples

### Flow: Shipment Created → Automatic Tracking Entry

```
ShipmentService publishes ShipmentCreatedEvent
  ↓ RabbitMQ
TrackingService / ShipmentCreatedConsumer.Consume()
  ├── Creates TrackingEvent:
  │   { status: "Created", location: "Mumbai", description: "Shipment created..." }
  └── SaveChangesAsync()

Customer calls GET /api/tracking/SS20260413154230001
  └── Returns timeline with "Created" entry
```

### Flow: Full Payment + Status Update Chain

```
PaymentCreatedEvent   → TrackingEvent { status: "PaymentInitiated" }
PaymentCompletedEvent → TrackingEvent { status: "PaymentVerified" }
ShipmentStatusUpdatedEvent (Draft→Booked)  → TrackingEvent { status: "Booked" }
ShipmentStatusUpdatedEvent (Booked→PickedUp) → TrackingEvent { status: "PickedUp" }
ShipmentStatusUpdatedEvent (→InTransit)    → TrackingEvent { status: "InTransit" }
ShipmentDeliveredEvent → TrackingEvent { status: "Delivered" }

Customer sees complete 6-event timeline in chronological order
```

---

## Interview-Ready Insights

### Potential Interview Questions

1. **"Why does TrackingService consume payment events?"**
   → To provide a unified timeline. Customers shouldn't need to check a separate payment history — the tracking page shows everything. This is a UX-driven architectural decision that creates some cross-domain coupling (tracking knows about payments), which is a deliberate trade-off.

2. **"How do you prevent duplicate tracking events from at-least-once delivery?"**
   → Deduplication query: same status + location within 5-minute window is rejected. This handles event broker replay but allows the same status to recur legitimately after the window.

3. **"Why store delivery proof images on the local filesystem instead of a database?"**
   → Binary data in relational databases: slow queries, poor scaling, high storage cost. Filesystem (or object storage like S3) is purpose-built for binary files. DB stores only the path.

4. **"How would you scale the Uploads directory in production?"**
   → Replace `Path.Combine(Directory.GetCurrentDirectory(), "Uploads")` with Azure Blob Storage / S3. The `Infrastructure` layer is the right place — swap `LocalFileStorageService` for `BlobStorageService` with the same interface. No Core/Domain changes needed.

5. **"What is the `GetAllPagedAsync` method used for?"**
   → Admin dashboard — paginated view of ALL tracking events across all shipments. Useful for operations monitoring.

### Potential Improvements

- **Cloud Object Storage:** Replace local `Uploads/` folder with Azure Blob Storage or AWS S3 for production scalability and durability.
- **Event Idempotency Key:** Instead of just time-window deduplication, store a content hash of the event and reject exact duplicates regardless of timing.
- **Delivery Proof Verification:** Currently no validation that the shipment is actually in `OutForDelivery` state before accepting proof. Should verify with ShipmentService.
- **Document Virus Scanning:** No malware scan on uploaded files. In production, integrate a ClamAV or Azure Defender scan before persisting.
- **Read-through Cache:** Tracking events are read-heavy (customers refresh constantly). Redis cache with short TTL (30 seconds) on `GetByTrackingNumberPagedAsync` would drastically reduce DB load.

### Trade-offs Made

| Decision | Trade-off |
|---|---|
| Local filesystem for uploads | Simple; no cloud dependency; not horizontally scalable |
| Contains() for status filter | Flexible partial match; unexpected results on input like "ed" |
| Tracking payment events | Unified UX view; cross-domain coupling |
| No auth check for tracking queries | Customers can query any tracking number; trade UX simplicity for access control |
