You are an expert C# / .NET AI agent helping a developer work on the
**Kantilever WarehouseService** — a warehouse/inventory microservice built with
ASP.NET Core 10, Entity Framework Core + PostgreSQL, and event-driven messaging
via Thumper (RabbitMQ).

Use the checklist below to orient yourself and guide the user.

---

## Environment Checklist

Work through these items in order. Mark each one `[x]` once confirmed.

### 1. Prerequisites
- [ ] A container engine is installed and running — Docker (`docker info`) or Podman (`podman info`); the start/test scripts auto-detect whichever is available, preferring Docker
- [ ] .NET SDK 10 is installed (`dotnet --version`) — optional; only useful for local development/debugging. The service and all test suites can be built and run exclusively via the container engine (see `Test.ps1` / `Test.sh`)

### 2. Infrastructure (containers)
- [ ] **warehouse-postgres** container is running on port 5432
- [ ] **warehouse-rabbitmq** container is running on port 5672 (management: 15672)

  > **Windows:** `Start.ps1`
  > **Linux / macOS:** `./Start.sh` (run `chmod +x Start.sh` once first)

### 3. Local NuGet feed
- [ ] `nuget.config` is present and points to `../_localFeed`
- [ ] `_localFeed/Thumper.Core.1.1.1.nupkg` exists
  _(obtain from the course facilitator or your team's package repository and place it in `labs/_localFeed/`)_

### 4. Build
- [ ] Solution builds without errors:
  `dotnet build Kantilever.WarehouseService.sln`

### 5. Database
- [ ] Database schema created and seed data loaded:
  `dotnet run --project Kantilever.WarehouseService.Initialization/...`
  (also done automatically by the start scripts)

### 6. Service
- [ ] Service runs and responds:
  `dotnet run --project Kantilever.WarehouseService/...`
- [ ] Swagger UI accessible: <http://localhost:5254/swagger>

### 7. Tests
- [ ] Unit tests pass:
  `dotnet test Kantilever.WarehouseService.Test/` or `./Test.sh unit` / `.\Test.ps1 unit`
- [ ] BDD spec tests pass:
  `dotnet test Kantilever.WarehouseService.Spec/` or `./Test.sh spec` / `.\Test.ps1 spec`
- [ ] Integration tests pass (requires Docker or Podman for TestContainers):
  `dotnet test Kantilever.WarehouseService.IntegrationTest/` or `./Test.sh integration` / `.\Test.ps1 integration`
  _(the containerized variant needs the engine's socket — see `Test.ps1` / `Test.sh` for details; falls back to a local SDK run if that's ever unavailable)_

---

## Quick-Start (new machine)

**Windows (PowerShell):**
```powershell
# From the Kantilever.WarehouseService folder:
.\Start.ps1
```

**Linux / macOS (bash):**
```bash
# From the Kantilever.WarehouseService folder:
chmod +x Start.sh   # first time only
./Start.sh
```

Both scripts do the same thing:
1. Detects an available container engine (Docker or Podman) and checks it's running
2. Pulls `postgres:latest` and `rabbitmq:management` images
3. Starts both containers (skips if already running)
4. Waits until PostgreSQL accepts connections
5. Builds the container images for the service and initialization
6. Runs `Kantilever.WarehouseService.Initialization` (in a container) to create the schema and seed data
7. Launches the web service (in a container) on <http://localhost:5254>

You should run the tests in a separate terminal to verify everything is working. If you don't have the .NET SDK installed, run them via the container engine instead:
```bash
# Fully containerized (no local .NET SDK required) — Unit + Spec + Integration:
./Test.sh          # Linux/macOS
.\Test.ps1         # Windows

# Or, with a local .NET SDK:
dotnet test Kantilever.WarehouseService.Test/
```

Next, you should use curl to interact with the API and verify it's working:
```bash
curl -X GET "http://localhost:5254/Warehouse?articleNumber=1"
```
This should return the stock information for article 1 (or a 404 if that article number doesn't exist yet).