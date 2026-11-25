# Kantilever WarehouseService - AI Agent Instructions

## Architecture Overview

This is a **microservice** for warehouse/inventory management in the Kantilever e-commerce system. The service follows **Domain-Driven Design** with clear separation of concerns:

- **Controllers/** - RESTful API endpoints (WarehouseController.cs)
- **Logic/** - Domain logic (IWarehouse interface, Warehouse implementation)
- **DAL/** - Data access layer with Entity Framework Core + PostgreSQL
- **Commands/** - CQRS-style command objects for operations
- **Events/** - Domain events published via Thumper messaging library
- **Model/** - Domain entities (ArticleStock)

## Key Domain Concepts

**Stock Management Operations:**
1. `FindArticleStockAsync` - Look up stock for an article
2. `PlaceStockInAsync` - Add stock to the warehouse
3. `RetrieveStockAsync` - Remove available stock
4. `ReserveStockAsync` - Reserve stock for an order
5. `ReserveStockEvenIfUnavailableAsync` - Reserve stock regardless of availability
6. `RetrieveReservedStockAsync` - Fulfill reserved stock
7. `ReleaseReservedStockAsync` - Release reserved stock

**Core Entity:** `ArticleStock` tracks `Stock` (total stock) and `OfWhichReserved` (reserved amount).

## Event-Driven Architecture

All operations publish domain events via **Thumper** (RabbitMQ abstraction):
- Events follow pattern: `Kantilever.WarehouseService.{EventName}`
- Example: `StockPlacedInWarehouse` contains `ArticleNumber`, `Quantity`, `NewStock`
- Events inherit from `DomainEvent` with `CorrelationId` for tracing

## Development Workflows

**Build & Run:**
```cmd
# Full build and container images (auto-detects Docker or Podman)
Make.cmd

# Individual builds
dotnet build Kantilever.WarehouseService/Kantilever.WarehouseService.csproj -c Release
```

**Testing Levels:**
1. **Unit Tests** (`Kantilever.WarehouseService.Test/`) - Business logic with mocks
2. **Integration Tests** (`IntegrationTest/`) - End-to-end with TestContainers (PostgreSQL + RabbitMQ)
3. **BDD Tests** (`Spec/`) - Reqnroll/Gherkin scenarios in English

Run all suites without a local .NET SDK via `Test.ps1` / `Test.sh` (auto-detects Docker or Podman); Unit and BDD tests are fully self-contained in a container, Integration tests additionally mount the engine's socket to run their own Testcontainers-managed Postgres/RabbitMQ (falls back to a local `dotnet test` if that doesn't work on your platform).

**Integration Test Pattern:**
```csharp
Given_AnArticleWith(articleNumber: 1, stock: 5, ofWhichReserved: 2);
await When_StockIsAdded(articleNumber: 1, quantity: 6);
Then_DeStockIs(articleNumber: 1, stock: 11, ofWhichReserved: 2); // note: method name is legacy Dutch
Then_EventWithCorrectContentWasPublished(...);
```

## Database & Infrastructure

- **PostgreSQL** database with Entity Framework Core
- **Connection String** from environment variable `WarehouseDB` or localhost default
- **Retry Policy** with Polly for database connection resilience
- **Docker or Podman** containers for both service and initialization (start scripts auto-detect the available engine)

## Project Structure Conventions

- **5 Projects:** Main service, Tests, Integration Tests, BDD Specs, Initialization
- **Dependency Injection** setup in `ProgramDIExtensions.cs`
- **Exception Handling:** Custom `FunctionalException` with structured `Error` objects
- **API Documentation:** Swagger/OpenAPI enabled in development

## Testing Dependencies

Integration tests use **TestContainers** for real PostgreSQL/RabbitMQ instances. Event verification uses `AutoResetEvent` with 2-second timeouts. The `EventData` class deserializes published events for assertions.

## Business Rules

- Available stock = `Stock - OfWhichReserved`
- Operations validate sufficient stock before execution
- All state changes must publish corresponding domain events
- Commands include `CorrelationId` for distributed tracing
