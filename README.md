# SmartShip Logistics Management System

SmartShip Logistics Management System is an enterprise-grade, microservice-based platform built for end-to-end logistics, parcel tracking, fleet and hub operations, payment processing, and notification handling. Engineered with .NET 10 Web APIs, Ocelot API Gateway, React 19, SQL Server, and RabbitMQ, SmartShip provides a resilient, event-driven solution to manage domestic and international shipments seamlessly.

---
## 📸 

### Home
![Home](https://github.com/ranasaurabh191/SafetySnap/blob/main/screenshots/Screenshot%202025-10-29%20141615.png)

### SignIn/SignUp
![SignIn/SignUp](https://github.com/ranasaurabh191/SmartShip-Logistics-Management-System/blob/main/Screenshot%202026-05-28%20000804.png)

### Admin Dashboard
![Dashboard](https://github.com/ranasaurabh191/SmartShip-Logistics-Management-System/blob/main/Screenshot%202026-05-28%20000824.png)

### Customer Dashboard
![Customer Dashboard](https://github.com/ranasaurabh191/SmartShip-Logistics-Management-System/blob/main/Screenshot%202026-05-28%20000844.png)

### ChatBot
![ChatBot](https://github.com/ranasaurabh191/SmartShip-Logistics-Management-System/blob/main/Screenshot%202026-05-28%20000956.png)

## Table of Contents

- [System Overview](#system-overview)
- [Architecture & Design](#architecture--design)
- [Microservices Overview](#microservices-overview)
- [Technology Stack](#technology-stack)
- [Repository Structure](#repository-structure)
- [Prerequisites](#prerequisites)
- [Quick Start with Docker Compose](#quick-start-with-docker-compose)
- [Manual Setup & Local Development](#manual-setup--local-development)
  - [Backend Services](#backend-services)
  - [Frontend Web Application](#frontend-web-application)
- [API Gateway Routes & Key Endpoints](#api-gateway-routes--key-endpoints)
- [Database Schema & Management](#database-schema--management)
- [Event-Driven Architecture & Messaging](#event-driven-architecture--messaging)
- [Authentication & Security](#authentication--security)
- [Payment Processing Flow](#payment-processing-flow)
- [Testing & Quality Assurance](#testing--quality-assurance)
- [CI/CD Pipeline](#cicd-pipeline)
- [Environment Configuration](#environment-configuration)
- [License](#license)

---

## System Overview

SmartShip Logistics Management System streamlines operations across the logistics lifecycle:
- Customer Parcel Management: Create, estimate rates for, and manage domestic/international shipments.
- Real-Time Package Tracking: Track shipment milestones, upload proof-of-delivery documents, and view location updates.
- Admin Hub & User Control: Manage distribution hubs, resolve shipment exceptions, analyze SLA metrics, and govern system roles.
- Secure Payment Gateway Integration: Process payments via Razorpay with signature verification and automated status updates.
- Asynchronous Event Notifications: Trigger real-time notifications (Email/SMS/In-app) via RabbitMQ on shipment status updates and payment events.

---

## Architecture & Design

SmartShip is architected using modular microservices, decoupled via event-driven messaging and centralized behind an API Gateway.

```
                  +-------------------------+
                  |  React 19 Frontend      |
                  |  (Vite + Tailwind CSS)  |
                  +------------+------------+
                               |
                               v
                  +-------------------------+
                  |   Ocelot API Gateway    |
                  |     (Port: 5000)        |
                  +------------+------------+
                               |
      +------------------------+------------------------+
      |            |           |           |            |
      v            v           v           v            v
+-----------+ +----------+ +----------+ +----------+ +-----------+
| Identity  | | Shipment | | Tracking | | Payment  | |   Admin   |
| Service   | | Service  | | Service  | | Service  | |  Service  |
| (5001)    | | (5002)   | | (5003)   | | (5005)   | |  (5004)   |
+-----+-----+ +----+-----+ +----+-----+ +----+-----+ +-----+-----+
      |            |            |            |             |
      +------------+------------+------------+-------------+
                               |
                 +-------------+-------------+
                 |                           |
                 v                           v
     +-----------------------+   +-----------------------+
     |  SQL Server 2022 DB   |   |   RabbitMQ Event Bus  |
     | (Per-service Schemas) |   |  (Notification 5006)  |
     +-----------------------+   +-----------------------+
```

---

## Microservices Overview

The platform consists of six specialized microservices, an API Gateway, and a web client:

1. SmartShip.Gateway (Port 5000)
   - Entry point for all client requests using Ocelot.
   - Handles path routing, JWT validation pass-through, CORS headers, and Swagger aggregation via MMLib.SwaggerForOcelot.

2. SmartShip.IdentityService (Port 5001)
   - Manages user registration, authentication, JWT token generation, and password hashing using BCrypt.
   - Supports Google and GitHub OAuth authentication providers.
   - Provides role-based user management endpoints.

3. SmartShip.ShipmentService (Port 5002)
   - Handles package creation, shipping rate calculations, pickup scheduling, and status lifecycle management.
   - Publishes shipment state transition events to RabbitMQ.

4. SmartShip.TrackingService (Port 5003)
   - Manages package tracking histories, milestone progress, hub check-ins, and proof-of-delivery file uploads.

5. SmartShip.AdminService (Port 5004)
   - Offers administrative controls over logistics hubs, SLA reporting, user account lifecycle, and shipment resolution overrides.

6. SmartShip.PaymentService (Port 5005)
   - Handles Razorpay order creation, payment status queries, and cryptographic payment signature verification.
   - Emits payment completion events for order processing.

7. SmartShip.NotificationService (Port 5006)
   - Listens to RabbitMQ event topics (Shipment Created, Status Updated, Payment Received) and dispatches automated notifications via MailKit / SMS integrations.

8. Frontend Web Application (Port 3000)
   - Single Page Application built with React 19, TypeScript, Material UI, Tailwind CSS, Framer Motion, and Leaflet maps.

---

## Technology Stack

### Backend & Core Infrastructure
- Framework: .NET 10.0 Web API
- API Gateway: Ocelot, MMLib.SwaggerForOcelot
- Data Access: Entity Framework Core 10.0, SQL Server 2022
- Message Broker: RabbitMQ 3.x with MassTransit
- Security & Auth: JWT Bearer Tokens, BCrypt.Net, Google & GitHub OAuth 2.0
- Logging & Diagnostics: Serilog (Console, File Sinks), ASP.NET Core Health Checks
- Validation: FluentValidation

### Frontend Client
- Library & Tooling: React 19, TypeScript, Vite, React Router DOM v7
- UI Framework & Styling: Material UI (MUI v9), Tailwind CSS, Framer Motion
- State & HTTP: Zustand, Axios
- Maps & Printing: Leaflet, React-Leaflet, jsPDF, html2canvas
- Testing: Vitest, React Testing Library, JSDOM

### DevOps & Automation
- Containerization: Docker, Docker Compose
- CI/CD Engine: Jenkins (Declarative pipeline with selective change detection)
- Web Server: Nginx (Containerized frontend host)

---

## Repository Structure

```
SmartShip Logistics Management System/
│
├── Gateway/
│   └── SmartShip.Gateway/               # Ocelot API Gateway (.NET 10)
│
├── Services/
│   ├── SmartShip.AdminService/          # Hubs, reports & administrative controls
│   ├── SmartShip.IdentityService/       # Auth, JWT, user & role management
│   ├── SmartShip.NotificationService/   # Email/SMS message consumers
│   ├── SmartShip.PaymentService/        # Razorpay payments & verification
│   ├── SmartShip.ShipmentService/       # Package lifecycle & rate calculation
│   └── SmartShip.TrackingService/       # Package tracking & proof of delivery
│
├── Shared/
│   └── SmartShip.Shared/                # Shared DTOs, contracts & event schemas
│
├── SmartShip.PaymentService.Tests/      # xUnit integration & unit tests
├── SmartShip.ShipmentService.Tests/     # xUnit integration & unit tests
│
├── frontend/                            # React 19 + TypeScript web application
│   ├── src/                             # Pages, components, services, state
│   ├── Dockerfile                       # Production Nginx multi-stage build
│   └── package.json                     # Frontend dependencies
│
├── SQL_Queries/                         # SQL scripts per microservice DB
├── docker-compose.yml                   # Multi-container orchestration
├── Jenkinsfile                          # CI/CD pipeline definition
├── ApiRequests.txt                      # Endpoint contract specs
└── README.md                            # Project documentation
```

---

## Prerequisites

Before running the project locally, ensure the following software is installed on your environment:

- Docker Desktop (v24.0+) and Docker Compose (v2.20+)
- .NET 10 SDK (for local CLI builds outside Docker)
- Node.js (v20.x or higher) and npm (v10.x or higher)
- SQL Server Management Studio (SSMS) or Azure Data Studio (optional, for direct DB inspection)
- Git

---

## Quick Start with Docker Compose

The simplest way to run the entire SmartShip ecosystem (all 6 microservices, gateway, frontend, SQL Server, and RabbitMQ) is using Docker Compose.

1. Clone the repository:
   ```bash
   git clone https://github.com/ranasaurabh191/SmartShip-Logistics-Management-System.git
   cd "SmartShip Logistics Management System"
   ```

2. Build and launch all services in detached mode:
   ```bash
   docker compose up --build -d
   ```

3. Verify running containers:
   ```bash
   docker compose ps
   ```

4. Access the application components:
   - Frontend Application: http://localhost:3000
   - API Gateway: http://localhost:5000
   - RabbitMQ Management Dashboard: http://localhost:15672 (Credentials: `guest` / `guest`)
   - SQL Server: localhost:1434 (SA Password: `SmartShip@123!`)

5. Stop all services and clean up containers:
   ```bash
   docker compose down -v
   ```

---

## Manual Setup & Local Development

### Backend Services

To run individual microservices locally using the .NET CLI:

1. Start infrastructure services (SQL Server and RabbitMQ):
   ```bash
   docker compose up -d sqlserver rabbitmq
   ```

2. Restore NuGet packages and run a target service (e.g., Identity Service):
   ```bash
   cd Services/SmartShip.IdentityService
   dotnet restore
   dotnet run
   ```

3. Run the API Gateway:
   ```bash
   cd Gateway/SmartShip.Gateway
   dotnet restore
   dotnet run
   ```

### Frontend Web Application

1. Navigate to the frontend directory:
   ```bash
   cd frontend
   ```

2. Install dependencies:
   ```bash
   npm install
   ```

3. Start the Vite development server:
   ```bash
   npm run dev
   ```
   The application will be accessible at http://localhost:5173 (or the port indicated in your console).

---

## API Gateway Routes & Key Endpoints

All requests route through the API Gateway at `http://localhost:5000/gateway/`. Below is an overview of key endpoints:

| Domain | Method | Endpoint Path | Authorization | Description |
| :--- | :--- | :--- | :--- | :--- |
| **Authentication** | `POST` | `/gateway/auth/signup` | Public | Register a new user account |
| **Authentication** | `POST` | `/gateway/auth/login` | Public | Authenticate user and issue JWT |
| **Shipments** | `POST` | `/gateway/shipments` | Authenticated | Create a new shipment |
| **Shipments** | `GET` | `/gateway/shipments/my` | Authenticated | Fetch current user's shipments |
| **Shipments** | `GET` | `/gateway/shipments/{id}` | Authenticated | Get detailed shipment metadata |
| **Shipments** | `GET` | `/gateway/shipments/rate` | Public | Calculate shipping rates |
| **Shipments** | `PATCH`| `/gateway/shipments/pickup/{id}`| Authenticated | Schedule pickup time |
| **Tracking** | `GET` | `/gateway/tracking/{trackingNo}`| Public | Get tracking history |
| **Payments** | `POST` | `/gateway/payment/create-order` | Authenticated | Initialize Razorpay order |
| **Payments** | `POST` | `/gateway/payment/verify` | Authenticated | Verify Razorpay payment signature |
| **Admin Operations**| `GET` | `/gateway/admin/dashboard` | Admin | Fetch system analytics |
| **Admin Operations**| `GET` | `/gateway/admin/hubs` | Admin | List all hub locations |
| **Admin Operations**| `POST` | `/gateway/admin/hubs` | Admin | Create a new distribution hub |
| **Admin Operations**| `GET` | `/gateway/admin/users` | Admin | Manage user accounts and roles |
| **Admin Operations**| `GET` | `/gateway/admin/reports` | Admin | Generate SLA and shipping reports |

---

## Database Schema & Management

SmartShip utilizes an isolated database pattern per microservice on MS SQL Server 2022. SQL initialization scripts are stored under the `/SQL_Queries` directory:

- `IdentityDb_SQLQuery.sql`: Users, roles, authentication credentials, and OAuth tokens.
- `ShipmentDb_SQLQuery.sql`: Shipments, addresses, package dimensions, status logs, and rates.
- `TrackingDb_SQLQuery.sql`: Checkpoints, tracking entries, and proof-of-delivery records.
- `PaymentDb_SQLQuery.sql`: Payment transactions, Razorpay order maps, amounts, and statuses.
- `AdminDb_SQLQuery.sql`: Distribution hubs, regional configurations, and SLA audit logs.
- `NotificationDb_SQLQuery.sql`: Sent notification history, email/SMS audit logs.

Entity Framework Core migrations manage schema migrations per service during startup or via `dotnet ef database update`.

---

## Event-Driven Architecture & Messaging

SmartShip leverages MassTransit over RabbitMQ to handle asynchronous processing and service decoupling:

1. **Shipment Created Event**: When a customer places a shipment, `ShipmentService` publishes a `ShipmentCreatedEvent`. `NotificationService` consumes it to send booking confirmation emails.
2. **Status Update Event**: Changing shipment status in `AdminService` or `TrackingService` dispatches a `ShipmentStatusChangedEvent`, triggering notifications to send tracking updates to recipients.
3. **Payment Verified Event**: Successful signature verification in `PaymentService` publishes `PaymentCompletedEvent`, notifying `ShipmentService` to transition status from `Pending Payment` to `Dispatched`.

---

## Authentication & Security

- JWT Bearer Authentication: Users authenticate at `/gateway/auth/login` and receive a signed JWT containing User ID, Email, and Roles (`ADMIN`, `CUSTOMER`).
- Role-Based Access Control (RBAC): Admin endpoints enforce explicit `[Authorize(Roles = "ADMIN")]` checks at the gateway and service level.
- Third-Party OAuth: Identity Service supports OAuth 2.0 integration for Google and GitHub logins.
- Data Protection: Passwords are hashed using BCrypt prior to database persistence.

---

## Payment Processing Flow

SmartShip integrates with Razorpay for secure payments:

1. Order Creation: The client calls `/gateway/payment/create-order`. The `PaymentService` calls Razorpay's API to generate a `razorpayOrderId`.
2. Checkout Execution: The frontend invokes Razorpay's SDK on the browser.
3. Verification: Upon payment completion, Razorpay returns payment details. The client submits `razorpayOrderId`, `razorpayPaymentId`, and `signature` to `/gateway/payment/verify`.
4. Cryptographic Validation: `PaymentService` computes the HMAC-SHA256 signature using the secret key to verify authenticity before committing the payment status.

---

## Testing & Quality Assurance

### Backend Tests
The backend features unit and integration tests written in xUnit with Moq, FluentAssertions, and EF Core InMemory provider:

- `SmartShip.PaymentService.Tests`: Tests payment order creation, signature validation, and status retrieval.
- `SmartShip.ShipmentService.Tests`: Tests shipment rate calculations, pickup scheduling, and status transitions.

Execute backend tests:
```bash
dotnet test SmartShip Logistics Management System.slnx
```

### Frontend Tests
The React frontend uses Vitest and React Testing Library:

Execute frontend tests:
```bash
cd frontend
npm run test
```

---

## CI/CD Pipeline

SmartShip uses a Jenkins pipeline defined in `Jenkinsfile`. The pipeline includes a change detection strategy to optimize build times by compiling and deploying only changed microservices:

1. Change Detection Stage: Compares Git commits (`git diff HEAD~1 HEAD`) to determine which services modified code.
2. Selective Container Build: Triggers targeted Docker builds (e.g., rebuilding only `SmartShip.IdentityService` if changes occurred solely within its project folder).
3. Full Rebuild Trigger: Executes a full `docker compose down && docker build` if global files like `docker-compose.yml`, `nuget.config`, or `Shared/` are modified.
4. Image Pruning: Automatically prunes dangling Docker images upon build completion.

---

## Environment Configuration

Configuration values are supplied via `appsettings.json` and environment variables. Key configuration variables include:

```env
# Database Settings
ConnectionStrings__DefaultConnection=Server=sqlserver,1433;Database=SmartShipDb;User Id=sa;Password=SmartShip@123!;TrustServerCertificate=True;

# Message Broker
RabbitMQ__Host=rabbitmq
RabbitMQ__Username=guest
RabbitMQ__Password=guest

# JWT Security
Jwt__Issuer=SmartShipIdentity
Jwt__Audience=SmartShipClients
Jwt__SecretKey=YourSuperSecretKeyWithMinimumLength32Chars!

# Payment Gateway (Razorpay)
Razorpay__KeyId=your_razorpay_key_id
Razorpay__KeySecret=your_razorpay_key_secret
```

---

## License

This project is proprietary and confidential. All rights reserved. Unauthorized copying, distribution, or modification of any files within this repository is strictly prohibited.
