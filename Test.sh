#!/usr/bin/env bash
# Runs the WarehouseService test suites via the container engine (Docker or
# Podman, auto-detected) - no local .NET SDK required for Unit and BDD Spec
# tests.
#
# Usage:
#   chmod +x Test.sh   (first time only)
#   ./Test.sh                # run Unit + Spec + Integration tests
#   ./Test.sh unit            # only Kantilever.WarehouseService.Test
#   ./Test.sh spec             # only Kantilever.WarehouseService.Spec
#   ./Test.sh integration       # only Kantilever.WarehouseService.IntegrationTest
#
# NOTE on Integration tests: that suite uses Testcontainers to start its own
# Postgres/RabbitMQ containers, so the test container needs access to the
# host's container engine (socket mount) and to reach those sibling
# containers over the network. This is handled via TESTCONTAINERS_HOST_OVERRIDE
# pointing at host.docker.internal (works on Docker Desktop out of the box;
# --add-host=host.docker.internal:host-gateway makes it resolve on native
# Linux Docker/Podman too). If it still fails on your platform, fall back to
# running it with a local .NET SDK: dotnet test Kantilever.WarehouseService.IntegrationTest/

set -euo pipefail

step()  { echo ""; echo "==> $*"; }
info()  { echo "    $*"; }
error() { echo "[ERROR] $*" >&2; exit 1; }

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
IMAGE="kantilever-warehouseservice-tests"
WHAT="${1:-all}"

step "Checking prerequisites"

if command -v docker >/dev/null 2>&1; then
    ENGINE=docker
elif command -v podman >/dev/null 2>&1; then
    ENGINE=podman
else
    error "Neither 'docker' nor 'podman' was found. Install Docker Desktop (https://www.docker.com/products/docker-desktop) or Podman (https://podman.io/docs/installation)."
fi

"$ENGINE" info >/dev/null 2>&1 || error "'$ENGINE' is installed but not reachable. Start the $ENGINE service/daemon first."

info "engine : $ENGINE ($("$ENGINE" --version))"

step "Checking local NuGet feed"
LOCAL_FEED="$SCRIPT_DIR/../_localFeed"
THUMPER_PKG="$LOCAL_FEED/Thumper.Core.1.1.1.nupkg"
if [ ! -f "$THUMPER_PKG" ]; then
    error "Missing required package: _localFeed/Thumper.Core.1.1.1.nupkg. Obtain this file from the course facilitator and place it in the '_localFeed' folder next to this solution."
fi
info "Thumper.Core.1.1.1.nupkg found."

# Stage the shared feed into the build context (Dockerfile.tests expects ./_localFeed here)
mkdir -p "$SCRIPT_DIR/_localFeed"
cp -R "$LOCAL_FEED/." "$SCRIPT_DIR/_localFeed/"

step "Building test image"
cd "$SCRIPT_DIR"
"$ENGINE" build -f Dockerfile.tests -t "$IMAGE" .

run_suite() {
    step "Running $1"
    "$ENGINE" run --rm "$IMAGE" test "$2/"
}

run_integration() {
    step "Running Integration tests (requires the $ENGINE socket)"
    local socket=""
    if [ "$ENGINE" = "docker" ]; then
        socket=$(docker context inspect --format '{{.Endpoints.docker.Host}}' 2>/dev/null | sed -e 's#^unix://##')
        if [ -z "$socket" ] || [ ! -S "$socket" ]; then
            socket="/var/run/docker.sock"
        fi
    else
        socket=$("$ENGINE" info --format '{{.Host.RemoteSocket.Path}}' 2>/dev/null || true)
    fi
    if [ -z "$socket" ] || [ ! -S "$socket" ]; then
        error "Could not find a usable $ENGINE socket for Testcontainers. For Podman, enable the socket service (e.g. 'systemctl --user enable --now podman.socket') or run this suite locally instead: dotnet test Kantilever.WarehouseService.IntegrationTest/"
    fi
    info "Using socket: $socket"
    if ! "$ENGINE" run --rm \
        --add-host=host.docker.internal:host-gateway \
        -v "$socket:/var/run/docker.sock" \
        -e DOCKER_HOST=unix:///var/run/docker.sock \
        -e TESTCONTAINERS_HOST_OVERRIDE=host.docker.internal \
        "$IMAGE" test Kantilever.WarehouseService.IntegrationTest/; then
        error "Integration tests failed inside the container. This is often a sibling-container networking issue. Fallback: run them locally instead: dotnet test Kantilever.WarehouseService.IntegrationTest/"
    fi
}

case "$WHAT" in
    unit)
        run_suite "Unit tests" "Kantilever.WarehouseService.Test"
        ;;
    spec)
        run_suite "BDD Spec tests" "Kantilever.WarehouseService.Spec"
        ;;
    integration)
        run_integration
        ;;
    all)
        run_suite "Unit tests" "Kantilever.WarehouseService.Test"
        run_suite "BDD Spec tests" "Kantilever.WarehouseService.Spec"
        run_integration
        ;;
    *)
        error "Unknown argument '$WHAT'. Use: unit | spec | integration | all"
        ;;
esac

step "Done."
