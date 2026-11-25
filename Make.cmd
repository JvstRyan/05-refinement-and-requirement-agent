rem "=======   Build application   ======="
dotnet build Kantilever.WarehouseService/Kantilever.WarehouseService.csproj -c Release
dotnet build Kantilever.WarehouseService.Initialization/Kantilever.WarehouseService.Initialization.csproj -c Release

rem "=======   publish application   ======="
dotnet publish --no-build -c Release -o ./Kantilever.WarehouseService/bin/Publish ./Kantilever.WarehouseService/Kantilever.WarehouseService.csproj
dotnet publish --no-build -c Release -o ./Kantilever.WarehouseService.Initialization/bin/Publish ./Kantilever.WarehouseService.Initialization/Kantilever.WarehouseService.Initialization.csproj

rem "=======   detect container engine (Docker or Podman)   ======="
set ENGINE=
where docker >nul 2>nul && set ENGINE=docker
if not defined ENGINE (
    where podman >nul 2>nul && set ENGINE=podman
)
if not defined ENGINE (
    echo [ERROR] Neither 'docker' nor 'podman' was found on PATH.
    exit /b 1
)

rem "=======   build container image   ======="
%ENGINE% build -t kantilever-warehouseservice-i .\Kantilever.WarehouseService
%ENGINE% build -t kantilever-warehouseservice-init-i .\Kantilever.WarehouseService.Initialization

