# ── build stage ────────────────────────────────────────────────────────────────
# Build context must be the solution root:
#   docker build -f Kantilever.WarehouseService.Initialization/Dockerfile -t kantilever-warehouseservice-init-i .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY nuget.config ./
COPY _localFeed/ /_localFeed/
COPY Kantilever.WarehouseService/Kantilever.WarehouseService.csproj ./Kantilever.WarehouseService/
COPY Kantilever.WarehouseService.Initialization/Kantilever.WarehouseService.Initialization.csproj ./Kantilever.WarehouseService.Initialization/
RUN dotnet restore ./Kantilever.WarehouseService.Initialization/Kantilever.WarehouseService.Initialization.csproj
COPY Kantilever.WarehouseService/ ./Kantilever.WarehouseService/
COPY Kantilever.WarehouseService.Initialization/ ./Kantilever.WarehouseService.Initialization/
RUN dotnet publish ./Kantilever.WarehouseService.Initialization/Kantilever.WarehouseService.Initialization.csproj \
    -c Release -o /app/publish --no-restore

# ── runtime stage ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Kantilever.WarehouseService.Initialization.dll"]