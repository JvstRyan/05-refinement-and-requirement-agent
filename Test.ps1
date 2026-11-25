#Requires -Version 5.1
<#
.SYNOPSIS
    Runs the WarehouseService test suites via the container engine (Docker or
    Podman, auto-detected) - no local .NET SDK required for Unit and BDD Spec
    tests.

.PARAMETER What
    Which suite(s) to run: all (default), unit, spec, or integration.

.EXAMPLE
    .\Test.ps1
.EXAMPLE
    .\Test.ps1 unit

.NOTES
    Integration tests use Testcontainers to start their own Postgres/RabbitMQ
    containers, so the test container needs access to the host's container
    engine (socket mount) and to reach those sibling containers over the
    network. This is handled via TESTCONTAINERS_HOST_OVERRIDE pointing at
    host.docker.internal (works on Docker Desktop out of the box;
    --add-host=host.docker.internal:host-gateway makes it resolve on native
    Linux Docker/Podman too). If it still fails on your platform, fall back
    to a local .NET SDK: dotnet test Kantilever.WarehouseService.IntegrationTest/
#>

param(
    [ValidateSet('all', 'unit', 'spec', 'integration')]
    [string]$What = 'all'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Step([string]$msg) {
    Write-Host ""
    Write-Host "==> $msg" -ForegroundColor Cyan
}

$ScriptDir = $PSScriptRoot
$Image = "kantilever-warehouseservice-tests"

# ── prerequisites ─────────────────────────────────────────────────────────────

Write-Step "Checking prerequisites"

$Engine = $null
if (Get-Command "docker" -ErrorAction SilentlyContinue) {
    $Engine = "docker"
} elseif (Get-Command "podman" -ErrorAction SilentlyContinue) {
    $Engine = "podman"
} else {
    Write-Host "[ERROR] Neither 'docker' nor 'podman' was found. Install Docker Desktop (https://www.docker.com/products/docker-desktop) or Podman (https://podman.io/docs/installation)." -ForegroundColor Red
    exit 1
}

& $Engine info > $null 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "[ERROR] '$Engine' is installed but not reachable. Start the $Engine service/daemon first." -ForegroundColor Red
    exit 1
}

Write-Host "  engine : $Engine ($(& $Engine --version))"

Write-Step "Checking local NuGet feed"
$LocalFeed = Join-Path $ScriptDir '..\_localFeed'
$ThumperPkg = Join-Path $LocalFeed 'Thumper.Core.1.1.1.nupkg'
if (-not (Test-Path $ThumperPkg)) {
    Write-Host "[ERROR] Missing required package: _localFeed\Thumper.Core.1.1.1.nupkg. Obtain this file from the course facilitator and place it in the '_localFeed' folder next to this solution." -ForegroundColor Red
    exit 1
}
Write-Host "  Thumper.Core.1.1.1.nupkg found."

# Stage the shared feed into the build context (Dockerfile.tests expects .\_localFeed here)
$LocalFeedTarget = Join-Path $ScriptDir '_localFeed'
if (-not (Test-Path $LocalFeedTarget)) {
    New-Item -ItemType Directory -Path $LocalFeedTarget | Out-Null
}
Copy-Item -Path (Join-Path $LocalFeed '*') -Destination $LocalFeedTarget -Recurse -Force

# ── build test image ─────────────────────────────────────────────────────────

Write-Step "Building test image"
Push-Location $ScriptDir
& $Engine build -f Dockerfile.tests -t $Image .
Pop-Location

function Run-Suite([string]$label, [string]$project) {
    Write-Step "Running $label"
    & $Engine run --rm $Image test "$project/"
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[ERROR] $label failed." -ForegroundColor Red
        exit 1
    }
}

function Run-Integration {
    Write-Step "Running Integration tests (requires the $Engine socket)"

    $Socket = $null
    if ($Engine -eq "docker") {
        $Socket = (& docker context inspect --format '{{.Endpoints.docker.Host}}' 2>&1 | Out-String).Trim() -replace '^unix://', ''
        if ([string]::IsNullOrWhiteSpace($Socket) -or -not (Test-Path $Socket)) {
            $Socket = "/var/run/docker.sock"
        }
    } else {
        $Socket = (& $Engine info --format '{{.Host.RemoteSocket.Path}}' 2>&1 | Out-String).Trim()
    }

    if ([string]::IsNullOrWhiteSpace($Socket)) {
        Write-Host "[ERROR] Could not find a usable $Engine socket for Testcontainers. For Podman, enable the socket service, or run this suite locally instead: dotnet test Kantilever.WarehouseService.IntegrationTest/" -ForegroundColor Red
        exit 1
    }

    Write-Host "  Using socket: $Socket"
    & $Engine run --rm `
        --add-host="host.docker.internal:host-gateway" `
        -v "${Socket}:/var/run/docker.sock" `
        -e "DOCKER_HOST=unix:///var/run/docker.sock" `
        -e "TESTCONTAINERS_HOST_OVERRIDE=host.docker.internal" `
        $Image test Kantilever.WarehouseService.IntegrationTest/
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[ERROR] Integration tests failed inside the container. This is often a sibling-container networking issue. Fallback: run them locally instead: dotnet test Kantilever.WarehouseService.IntegrationTest/" -ForegroundColor Red
        exit 1
    }
}

# ── run requested suite(s) ───────────────────────────────────────────────────

switch ($What) {
    'unit'        { Run-Suite "Unit tests" "Kantilever.WarehouseService.Test" }
    'spec'        { Run-Suite "BDD Spec tests" "Kantilever.WarehouseService.Spec" }
    'integration' { Run-Integration }
    'all' {
        Run-Suite "Unit tests" "Kantilever.WarehouseService.Test"
        Run-Suite "BDD Spec tests" "Kantilever.WarehouseService.Spec"
        Run-Integration
    }
}

Write-Step "Done."
