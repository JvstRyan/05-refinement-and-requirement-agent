#Requires -Version 5.1
<#
.SYNOPSIS
    One-shot bootstrap for Kantilever.WarehouseService.
    Pulls images, starts infrastructure, builds the container images,
    initialises the database, and launches the web service.
    Everything is built and run exclusively through a container engine
    (Docker or Podman, auto-detected); no local .NET SDK is required.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# ── helpers ──────────────────────────────────────────────────────────────────

function Write-Step([string]$msg) {
    Write-Host ""
    Write-Host "==> $msg" -ForegroundColor Cyan
}

function Container-IsRunning([string]$name) {
    try {
        $state = & $Engine inspect --format '{{.State.Status}}' $name 2>&1
        return ($state -eq 'running')
    } catch {
        return $false
    }
}

function Container-Exists([string]$name) {
    try {
        & $Engine inspect $name 2>&1 | Out-Null
        return $true
    } catch {
        return $false
    }
}

# ── prerequisites ─────────────────────────────────────────────────────────────

Write-Step "Checking prerequisites"

# Detect an available container engine: prefer Docker, fall back to Podman
$Engine = $null
if (Get-Command "docker" -ErrorAction SilentlyContinue) {
    $Engine = "docker"
} elseif (Get-Command "podman" -ErrorAction SilentlyContinue) {
    $Engine = "podman"
} else {
    Write-Host "[ERROR] Neither 'docker' nor 'podman' was found. Install Docker Desktop (https://www.docker.com/products/docker-desktop) or Podman (https://podman.io/docs/installation)." -ForegroundColor Red
    exit 1
}

# Verify the container engine's daemon/service is reachable
& $Engine info > $null 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "[ERROR] '$Engine' is installed but not reachable. Start the $Engine service/daemon first." -ForegroundColor Red
    exit 1
}

Write-Host "  engine : $Engine ($(& $Engine --version))"

# ── configuration ─────────────────────────────────────────────────────────────

$POSTGRES_CONTAINER  = "warehouse-postgres"
$RABBITMQ_CONTAINER  = "warehouse-rabbitmq"
$SERVICE_CONTAINER   = "kantilever-warehouseservice-c"
$CONTAINER_NETWORK      = "warehouse-net"
$POSTGRES_PASSWORD   = "Geheim_101"
$POSTGRES_USER       = "postgres"
$POSTGRES_DB         = "WarehouseDB"
$SERVICE_PORT        = 5254
# Connection string used by containers (host = container name on the network)
$DB_CONN_CONTAINER      = "Host=$POSTGRES_CONTAINER;Port=5432;Database=$POSTGRES_DB;Username=$POSTGRES_USER;Password=$POSTGRES_PASSWORD"

$ScriptDir = $PSScriptRoot

# ── pull images ───────────────────────────────────────────────────────────────

Write-Step "Pulling container images (postgres, rabbitmq:management)"
& $Engine pull postgres:latest
& $Engine pull rabbitmq:management

# ── container network ──────────────────────────────────────────────────────────

Write-Step "Ensuring container network ($CONTAINER_NETWORK)"
$netExists = & $Engine network ls --format '{{.Name}}' 2>&1 | Where-Object { $_ -eq $CONTAINER_NETWORK }
if (-not $netExists) {
    & $Engine network create $CONTAINER_NETWORK | Out-Null
    Write-Host "  Created network $CONTAINER_NETWORK."
} else {
    Write-Host "  Network $CONTAINER_NETWORK already exists."
}

# ── start PostgreSQL ──────────────────────────────────────────────────────────

Write-Step "Starting PostgreSQL container ($POSTGRES_CONTAINER)"
if (Container-IsRunning $POSTGRES_CONTAINER) {
    Write-Host "  Already running – skipping."
} elseif (Container-Exists $POSTGRES_CONTAINER) {
    Write-Host "  Container exists but stopped – starting."
    & $Engine start $POSTGRES_CONTAINER
} else {
    & $Engine run -d `
        --name $POSTGRES_CONTAINER `
        --network $CONTAINER_NETWORK `
        -e POSTGRES_PASSWORD=$POSTGRES_PASSWORD `
        -e POSTGRES_USER=$POSTGRES_USER `
        -e POSTGRES_DB=$POSTGRES_DB `
        -p 5432:5432 `
        postgres:latest
}

# ── start RabbitMQ ────────────────────────────────────────────────────────────

Write-Step "Starting RabbitMQ container ($RABBITMQ_CONTAINER)"
if (Container-IsRunning $RABBITMQ_CONTAINER) {
    Write-Host "  Already running – skipping."
} elseif (Container-Exists $RABBITMQ_CONTAINER) {
    Write-Host "  Container exists but stopped – starting."
    & $Engine start $RABBITMQ_CONTAINER
} else {
    & $Engine run -d `
        --name $RABBITMQ_CONTAINER `
        --network $CONTAINER_NETWORK `
        -p 5672:5672 `
        -p 15672:15672 `
        rabbitmq:management
}

# ── wait for PostgreSQL to be ready ──────────────────────────────────────────

Write-Step "Waiting for PostgreSQL to accept connections"
$retries = 20
for ($i = 1; $i -le $retries; $i++) {
    $result = & $Engine exec $POSTGRES_CONTAINER pg_isready -U $POSTGRES_USER 2>&1
    if ($result -match "accepting connections") {
        Write-Host "  PostgreSQL is ready."
        break
    }
    if ($i -eq $retries) {
        Write-Host "[ERROR] PostgreSQL did not become ready in time." -ForegroundColor Red
        exit 1
    }
    Write-Host "  Waiting... ($i/$retries)"
    Start-Sleep -Seconds 2
}

# ── check local NuGet feed ───────────────────────────────────────────────────

Write-Step "Checking local NuGet feed"
$LocalFeed = Join-Path $ScriptDir '..\_localFeed'
$ThumperPkg = Join-Path $LocalFeed 'Thumper.Core.1.1.1.nupkg'
if (-not (Test-Path $ThumperPkg)) {
    Write-Host "[ERROR] Missing required package: _localFeed\Thumper.Core.1.1.1.nupkg" -ForegroundColor Red
    Write-Host "        Obtain this file from the course facilitator and place it in the '_localFeed' folder next to this solution." -ForegroundColor Yellow
    exit 1
}
Write-Host "  Thumper.Core.1.1.1.nupkg found."

# ── stage local NuGet feed into the build context ────────────────────────────
# The Dockerfiles expect ./_localFeed inside the solution folder (the build
# context), but the shared feed lives one level up - stage a copy here.
$LocalFeedTarget = Join-Path $ScriptDir '_localFeed'
if (-not (Test-Path $LocalFeedTarget)) {
    New-Item -ItemType Directory -Path $LocalFeedTarget | Out-Null
}
Copy-Item -Path (Join-Path $LocalFeed '*') -Destination $LocalFeedTarget -Recurse -Force

# ── build ─────────────────────────────────────────────────────────────────────

Write-Step "Building container images"
Push-Location $ScriptDir
& $Engine build -f Kantilever.WarehouseService/Dockerfile `
    -t kantilever-warehouseservice-i .
& $Engine build -f Kantilever.WarehouseService.Initialization/Dockerfile `
    -t kantilever-warehouseservice-init-i .
Pop-Location

# ── initialise database ───────────────────────────────────────────────────────

Write-Step "Initialising database (container)"
& $Engine run --rm `
    --network $CONTAINER_NETWORK `
    -e "WarehouseDB=$DB_CONN_CONTAINER" `
    kantilever-warehouseservice-init-i
if ($LASTEXITCODE -ne 0) {
    Write-Host "[ERROR] Database initialisation failed." -ForegroundColor Red
    exit 1
}

# ── start web service ─────────────────────────────────────────────────────────

Write-Step "Starting WarehouseService on http://localhost:$SERVICE_PORT"
Write-Host "  Swagger UI : http://localhost:$SERVICE_PORT/swagger"
Write-Host "  RabbitMQ   : http://localhost:15672  (guest / guest)"
Write-Host ""

if (Container-Exists $SERVICE_CONTAINER) {
    Write-Host "  Removing stale service container..."
    & $Engine rm -f $SERVICE_CONTAINER | Out-Null
}
& $Engine run -d `
    --name $SERVICE_CONTAINER `
    --network $CONTAINER_NETWORK `
    -e "WarehouseDB=$DB_CONN_CONTAINER" `
    -e "ASPNETCORE_ENVIRONMENT=Development" `
    -p "${SERVICE_PORT}:8080" `
    kantilever-warehouseservice-i
Write-Host "  Service running in background (container: $SERVICE_CONTAINER)"
Write-Host "  To stop: $Engine stop $SERVICE_CONTAINER" -ForegroundColor Yellow
