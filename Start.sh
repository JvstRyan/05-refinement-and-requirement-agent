#!/usr/bin/env bash
# One-shot bootstrap for Kantilever.WarehouseService.
# Pulls images, starts infrastructure, builds the container images,
# initialises the database, and launches the web service.
# Everything is built and run exclusively through a container engine
# (Docker or Podman, auto-detected); no local .NET SDK is required.
#
# Usage:
#   chmod +x Start.sh   (first time only)
#   ./Start.sh

set -euo pipefail

# ── helpers ───────────────────────────────────────────────────────────────────

step()  { echo ""; echo "==> $*"; }
info()  { echo "    $*"; }
error() { echo "[ERROR] $*" >&2; exit 1; }

container_is_running() {
    local state
    state=$("$ENGINE" inspect --format '{{.State.Status}}' "$1" 2>/dev/null || true)
    [ "$state" = "running" ]
}

container_exists() {
    local id
    id=$("$ENGINE" ps -aq --filter "name=^$1$" 2>/dev/null || true)
    [ -n "$id" ]
}

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# ── configuration ─────────────────────────────────────────────────────────────

POSTGRES_CONTAINER="warehouse-postgres"
RABBITMQ_CONTAINER="warehouse-rabbitmq"
SERVICE_CONTAINER="kantilever-warehouseservice-c"
CONTAINER_NETWORK="warehouse-net"
POSTGRES_PASSWORD="Geheim_101"
POSTGRES_USER="postgres"
POSTGRES_DB="WarehouseDB"
SERVICE_PORT=5254
# Connection string used by containers (host = container name on the network)
DB_CONN_CONTAINER="Host=${POSTGRES_CONTAINER};Port=5432;Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}"

# ── prerequisites ─────────────────────────────────────────────────────────────

step "Checking prerequisites"

# Detect an available container engine: prefer Docker, fall back to Podman
if command -v docker >/dev/null 2>&1; then
    ENGINE=docker
elif command -v podman >/dev/null 2>&1; then
    ENGINE=podman
else
    error "Neither 'docker' nor 'podman' was found. Install Docker Desktop (https://www.docker.com/products/docker-desktop) or Podman (https://podman.io/docs/installation)."
fi

"$ENGINE" info >/dev/null 2>&1 || error "'$ENGINE' is installed but not reachable. Start the $ENGINE service/daemon first."

info "engine : $ENGINE ($("$ENGINE" --version))"

# ── pull images ───────────────────────────────────────────────────────────────

step "Pulling container images (postgres, rabbitmq:management)"
"$ENGINE" pull postgres:latest
"$ENGINE" pull rabbitmq:management
# ── container network ─────────────────────────────────────────────────

step "Ensuring container network ($CONTAINER_NETWORK)"
"$ENGINE" network create "$CONTAINER_NETWORK" 2>/dev/null || true
info "Network $CONTAINER_NETWORK ready."
# ── start PostgreSQL ──────────────────────────────────────────────────────────

step "Starting PostgreSQL container ($POSTGRES_CONTAINER)"
if container_is_running "$POSTGRES_CONTAINER"; then
    info "Already running – skipping."
elif container_exists "$POSTGRES_CONTAINER"; then
    info "Container exists but stopped – starting."
    "$ENGINE" start "$POSTGRES_CONTAINER"
else
    "$ENGINE" run -d \
        --name "$POSTGRES_CONTAINER" \
        --network "$CONTAINER_NETWORK" \
        -e POSTGRES_PASSWORD="$POSTGRES_PASSWORD" \
        -e POSTGRES_USER="$POSTGRES_USER" \
        -e POSTGRES_DB="$POSTGRES_DB" \
        -p 5432:5432 \
        postgres:latest
fi

# ── start RabbitMQ ────────────────────────────────────────────────────────────

step "Starting RabbitMQ container ($RABBITMQ_CONTAINER)"
if container_is_running "$RABBITMQ_CONTAINER"; then
    info "Already running – skipping."
elif container_exists "$RABBITMQ_CONTAINER"; then
    info "Container exists but stopped – starting."
    "$ENGINE" start "$RABBITMQ_CONTAINER"
else
    "$ENGINE" run -d \
        --name "$RABBITMQ_CONTAINER" \
        --network "$CONTAINER_NETWORK" \
        -p 5672:5672 \
        -p 15672:15672 \
        rabbitmq:management
fi

# ── wait for PostgreSQL to be ready ──────────────────────────────────────────

step "Waiting for PostgreSQL to accept connections"
retries=20
for i in $(seq 1 $retries); do
    if "$ENGINE" exec "$POSTGRES_CONTAINER" pg_isready -U "$POSTGRES_USER" 2>/dev/null | grep -q "accepting connections"; then
        info "PostgreSQL is ready."
        break
    fi
    if [ "$i" -eq "$retries" ]; then
        error "PostgreSQL did not become ready in time."
    fi
    info "Waiting... ($i/$retries)"
    sleep 2
done

# ── check local NuGet feed ───────────────────────────────────────────────────

step "Checking local NuGet feed"
LOCAL_FEED="$SCRIPT_DIR/../_localFeed"
THUMPER_PKG="$LOCAL_FEED/Thumper.Core.1.1.1.nupkg"
if [ ! -f "$THUMPER_PKG" ]; then
    echo "[ERROR] Missing required package: _localFeed/Thumper.Core.1.1.1.nupkg" >&2
    echo "        Obtain this file from the course facilitator and place it in the '_localFeed' folder next to this solution." >&2
    exit 1
fi
info "Thumper.Core.1.1.1.nupkg found."

# ── stage local NuGet feed into the build context ────────────────────────────
# The Dockerfiles expect ./_localFeed inside the solution folder (the build
# context), but the shared feed lives one level up - stage a copy here.
mkdir -p "$SCRIPT_DIR/_localFeed"
cp -R "$LOCAL_FEED/." "$SCRIPT_DIR/_localFeed/"

# ── build ─────────────────────────────────────────────────────────────────────

step "Building container images"
cd "$SCRIPT_DIR"
"$ENGINE" build -f Kantilever.WarehouseService/Dockerfile \
    -t kantilever-warehouseservice-i .
"$ENGINE" build -f Kantilever.WarehouseService.Initialization/Dockerfile \
    -t kantilever-warehouseservice-init-i .

# ── initialise database ───────────────────────────────────────────────────────

step "Initialising database (container)"
"$ENGINE" run --rm \
    --network "$CONTAINER_NETWORK" \
    -e "WarehouseDB=$DB_CONN_CONTAINER" \
    kantilever-warehouseservice-init-i || error "Database initialisation failed."

# ── start web service ─────────────────────────────────────────────────────────

step "Starting WarehouseService on http://localhost:$SERVICE_PORT"
info "Swagger UI : http://localhost:$SERVICE_PORT/swagger"
info "RabbitMQ   : http://localhost:15672  (guest / guest)"
echo ""

"$ENGINE" rm -f "$SERVICE_CONTAINER" 2>/dev/null || true
"$ENGINE" run -d \
    --name "$SERVICE_CONTAINER" \
    --network "$CONTAINER_NETWORK" \
    -e "WarehouseDB=$DB_CONN_CONTAINER" \
    -e "ASPNETCORE_ENVIRONMENT=Development" \
    -p "${SERVICE_PORT}:8080" \
    kantilever-warehouseservice-i
info "Service running in background (container: $SERVICE_CONTAINER)"
echo "    To stop: $ENGINE stop $SERVICE_CONTAINER"
