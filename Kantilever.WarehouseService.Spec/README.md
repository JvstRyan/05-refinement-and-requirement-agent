# Kantilever WarehouseService - Reqnroll Feature Tests

The feature files voor de Kantilever WarehouseService moeten worden geschreven in het Nederlands, aangezien de service zelf in het Nederlands is ontwikkeld en de gebruikers van de service waarschijnlijk ook Nederlandstalig zijn. Het gebruik van Nederlandse Gherkin-woorden zorgt voor een betere leesbaarheid en begrijpelijkheid van de tests voor alle betrokkenen, inclusief ontwikkelaars, testers en stakeholders.

## Project Structure

### Feature Files (`Features/`)
- **StockBeheer.feature** - Stockbeheer scenario's
- **StockReservering.feature** - Stockreservering scenario's  

### Step Definitions (`StepDefinitions/`)
- **StockBeheerStepDefinitions.cs** - Stockbeheer stappen
- **StockReserveringStepDefinitions.cs** - Stockreservering stappen

### Configuration
- **reqnroll.json** - Reqnroll configuration
- **Kantilever.WarehouseService.Spec.csproj** - Project dependencies

## Test Coverage

The feature files cover the following scenarios:

### Stockbeheer (StockBeheer.feature)
- Haal stockinformatie op voor bestaande/niet-bestaande artikelen
- Voeg stock toe aan bestaande en nieuwe artikelen
- Verwijder stock met voldoende/onvoldoende hoeveelheden
- Invoervalidatie (negatieve hoeveelheden)
- Verificatie van event publicatie

### Stockreservering (StockReservering.feature)  
- Reserveer stock met voldoende beschikbare stock
- Afhandelen van scenario's met onvoldoende stock
- Reserveer exacte hoeveelheden en randgevallen
- Validatie van negatieve reserveringspogingen



## Test Framework Setup

### Dependencies
- **Reqnroll** - BDD testing framework
- **FluentAssertions** - Assertion library
- **Moq** - Mocking framework
- **xUnit** - Test runner
- **Kantilever.WarehouseService** - Service under test

### Configuration Features
- Dutch Gherkin keywords
- xUnit test runner integration
- Mock-based testing approach
- Scenario isolation and cleanup

## Running the Tests

```bash
# Build the project
dotnet build

# Run all tests
dotnet test
```

