# ── build stage ────────────────────────────────────────────────────────────────
# Build context must be the solution root:
#   docker build -f Kantilever.WarehouseService/Dockerfile -t kantilever-warehouseservice-i .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY nuget.config ./
COPY _localFeed/ /_localFeed/
COPY Kantilever.WarehouseService/Kantilever.WarehouseService.csproj ./Kantilever.WarehouseService/
RUN dotnet restore ./Kantilever.WarehouseService/Kantilever.WarehouseService.csproj
COPY Kantilever.WarehouseService/ ./Kantilever.WarehouseService/
RUN dotnet publish ./Kantilever.WarehouseService/Kantilever.WarehouseService.csproj \
    -c Release -o /app/publish --no-restore

# ── runtime stage ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Kantilever.WarehouseService.dll"]