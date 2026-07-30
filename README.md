# SmartShip - Microservices-Based Logistics Management System

## Overview

SmartShip is a full-stack microservices-based logistics management system built with ASP.NET Core and React. The system provides end-to-end logistics operations management including shipment tracking, payment processing, and real-time notifications through a distributed architecture.

## Architecture

The system follows a microservices architecture pattern with the following characteristics:

- **Service Decomposition**: Each business capability is implemented as an independent service
- **Data Ownership**: Each microservice owns its database and data schema
- **Communication**: Services communicate through synchronous REST APIs and asynchronous messaging via RabbitMQ
- **Containerization**: All services are containerized using Docker for consistent deployment
- **Orchestration**: Docker Compose for local development; Kubernetes-ready for production


## Technology Stack

### Backend

- **Framework**: ASP.NET Core 8.0
- **Language**: C\#
- **Database**: SQL Server with Entity Framework Core (Code First approach)
- **ORM**: Entity Framework Core with Repository and Unit of Work patterns
- **Messaging**: RabbitMQ with MassTransit for event-driven communication
- **Authentication**: JWT Bearer tokens with role-based authorization
- **API Documentation**: Swagger/OpenAPI
- **Payment Integration**: Razorpay gateway


### Frontend

- **Framework**: React 18 with TypeScript
- **State Management**: React Context API / Redux Toolkit
- **UI Library**: Material-UI / Tailwind CSS
- **HTTP Client**: Axios with interceptors
- **Real-time Updates**: SignalR client integration


### DevOps \& Infrastructure

- **Containerization**: Docker, Docker Compose
- **CI/CD**: Jenkins pipelines / GitHub Actions
- **Version Control**: Git with GitHub
- **API Testing**: Postman collections
- **Database Management**: SQL Server Management Studio
- **Message Queue Monitoring**: RabbitMQ Management Console
- **Logging**: Centralized logging with Serilog


## Microservices

| Service | Responsibility | Technology |
| :-- | :-- | :-- |
| **API Gateway** | Request routing, authentication, rate limiting | ASP.NET Core, Ocelot/YARP |
| **Auth Service** | User management, JWT token issuance, roles | ASP.NET Core Identity, JWT |
| **Shipment Service** | Shipment CRUD, tracking, status updates | ASP.NET Core, EF Core, SQL Server |
| **Payment Service** | Payment processing, Razorpay integration | ASP.NET Core, Razorpay SDK |
| **Notification Service** | Email/SMS/push notifications | ASP.NET Core, MassTransit, RabbitMQ |
| **Tracking Service** | Real-time location tracking, history | ASP.NET Core, SignalR |
| **Reporting Service** | Analytics, dashboards, export | ASP.NET Core, EF Core |

## Project Structure

```
SmartShip/
├── src/
│   ├── SmartShip.ApiGateway/
│   ├── SmartShip.AuthService/
│   ├── SmartShip.ShipmentService/
│   ├── SmartShip.PaymentService/
│   ├── SmartShip.NotificationService/
│   ├── SmartShip.TrackingService/
│   ├── SmartShip.ReportingService/
│   └── SmartShip.Shared/
│       ├── SmartShip.Shared.Contracts/
│       ├── SmartShip.Shared.Kernel/
│       └── SmartShip.Shared.Messaging/
├── frontend/
│   └── smartship-web/
├── docker-compose.yml
├── docker-compose.override.yml
├── Jenkinsfile
└── README.md
```


## Getting Started

### Prerequisites

- .NET 8.0 SDK
- Node.js 18+ and npm
- Docker Desktop
- SQL Server 2019+ (or Docker container)
- RabbitMQ (or Docker container)


### Local Development Setup

1. **Clone the repository**

```bash
git clone <repository-url>
cd SmartShip
```

2. **Start infrastructure services**

```bash
docker-compose up -d sqlserver rabbitmq
```

3. **Configure connection strings**
    - Update `appsettings.Development.json` in each service
    - Configure SQL Server connection strings
    - Configure RabbitMQ connection settings
    - Configure JWT secret keys
    - Configure Razorpay API keys
4. **Run database migrations**

```bash
# From each service directory
dotnet ef database update
```

5. **Start backend services**

```bash
# Option 1: Run all services via Docker Compose
docker-compose up -d

# Option 2: Run individually for debugging
dotnet run --project src/SmartShip.AuthService
dotnet run --project src/SmartShip.ShipmentService
# ... other services
```

6. **Start frontend**

```bash
cd frontend/smartship-web
npm install
npm start
```

7. **Access applications**
    - Frontend: http://localhost:3000
    - API Gateway: http://localhost:5000
    - Swagger UI: http://localhost:5000/swagger
    - RabbitMQ Management: http://localhost:15672

## Key Features

### Shipment Management

- Create, read, update, delete shipments
- Real-time status tracking (Created, In Transit, Out for Delivery, Delivered, Exception)
- Shipment history and audit trail
- Multi-carrier support


### Payment Processing

- Razorpay integration for secure payments
- Payment status tracking (Pending, Completed, Failed, Refunded)
- Webhook handling for payment confirmations
- Invoice generation


### Authentication \& Authorization

- JWT-based authentication
- Role-based access control (Admin, Manager, Driver, Customer)
- Refresh token rotation
- Password reset workflows


### Event-Driven Communication

- Saga pattern for distributed transactions
- Outbox pattern for reliable message publishing
- Dead letter queue handling
- Message retry policies


### Real-Time Notifications

- SignalR hubs for live updates
- Email notifications via SMTP
- SMS integration ready
- In-app notification center


## API Documentation

Each microservice exposes its own Swagger documentation at `/swagger` endpoint. The API Gateway aggregates documentation for a unified view.

### Core Endpoints

| Service | Base Path | Key Endpoints |
| :-- | :-- | :-- |
| Auth | `/api/auth` | `POST /login`, `POST /register`, `POST /refresh` |
| Shipment | `/api/shipments` | `GET /`, `POST /`, `GET /{id}`, `PUT /{id}`, `GET /{id}/tracking` |
| Payment | `/api/payments` | `POST /`, `GET /{id}`, `POST /webhook` |
| Tracking | `/api/tracking` | `GET /{shipmentId}`, `GET /{shipmentId}/history` |

## Testing

### Unit Tests

```bash
dotnet test --filter "Category=Unit"
```


### Integration Tests

```bash
dotnet test --filter "Category=Integration"
```


### API Tests (Postman)

Import the Postman collection from `/docs/postman/SmartShip.postman_collection.json`

## Deployment

### Docker Images

Each service builds a multi-stage Docker image optimized for production.

```bash
# Build all images
docker-compose -f docker-compose.yml -f docker-compose.prod.yml build

# Push to registry
docker-compose push
```


### Environment Variables

Key environment variables for production:

- `ASPNETCORE_ENVIRONMENT=Production`
- `ConnectionStrings__DefaultConnection`
- `RabbitMQ__Host`
- `Jwt__SecretKey`
- `Razorpay__KeyId`
- `Razorpay__KeySecret`


## Monitoring \& Observability

- **Logging**: Structured logging with Serilog (JSON format)
- **Metrics**: Prometheus metrics endpoint at `/metrics`
- **Health Checks**: `/health` endpoint for each service
- **Distributed Tracing**: Ready for OpenTelemetry integration


## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit changes (`git commit -m 'Add amazing feature'`)
4. Push to branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Code Standards

- Follow .NET coding conventions
- Write unit tests for new functionality
- Update API documentation for contract changes
- Run static analysis (`dotnet format`)

## Contact

Saurabh Rana - [GitHub Profile](https://github.com/saurabhrana)

Project Link: [https://github.com/saurabhrana/SmartShip](https://github.com/saurabhrana/SmartShip)

***

## Acknowledgments

- Microsoft eShopOnContainers reference architecture
- ASP.NET Core documentation
- RabbitMQ and MassTransit communities
- Razorpay developer documentation
<span style="display:none">[^1][^10][^11][^12][^13][^14][^15][^16][^17][^18][^19][^2][^20][^21][^22][^23][^24][^25][^26][^27][^28][^29][^3][^30][^4][^5][^6][^7][^8][^9]</span>

