using Kantilever.WarehouseService.Commands;
using Kantilever.WarehouseService.Controllers;
using Kantilever.WarehouseService.DAL;
using Kantilever.WarehouseService.Events;
using Kantilever.WarehouseService.Logic;
using Kantilever.WarehouseService.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Reqnroll;
using Thumper.Core;

namespace Kantilever.WarehouseService.Spec.Features;

[Binding]
public class WarehouseSteps
{
    private SqliteConnection _connection = null!;
    private DbContextOptions<WarehouseContext> _options = null!;
    private Mock<IThumperPublisher> _publisherMock = null!;
    private IActionResult _lastResult = null!;
    private readonly List<object> _publishedEvents = [];

    [BeforeScenario]
    public void SetupScenario()
    {
        _connection = new SqliteConnection("datasource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<WarehouseContext>()
            .UseSqlite(_connection)
            .Options;

        using var ctx = new WarehouseContext(_options);
        ctx.Database.EnsureCreated();

        _publisherMock = new Mock<IThumperPublisher>(MockBehavior.Loose);
        _publisherMock
            .Setup(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<StockPlacedInWarehouse>()))
            .Callback<string, StockPlacedInWarehouse>((_, e) => _publishedEvents.Add(e))
            .Returns(Task.CompletedTask);
        _publisherMock
            .Setup(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<StockRetrievedFromWarehouse>()))
            .Callback<string, StockRetrievedFromWarehouse>((_, e) => _publishedEvents.Add(e))
            .Returns(Task.CompletedTask);
        _publisherMock
            .Setup(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<StockReserved>()))
            .Callback<string, StockReserved>((_, e) => _publishedEvents.Add(e))
            .Returns(Task.CompletedTask);
    }

    [AfterScenario]
    public void CleanupScenario()
    {
        _connection?.Close();
        _connection?.Dispose();
    }

    private WarehouseContext CreateContext() => new(_options);

    private WarehouseController CreateController() =>
        new(new Warehouse(CreateContext(), _publisherMock.Object));

    // ── Given ───────────────────────────────────────────────────────────────

    [Given(@"^the warehouse system is available$")]
    public void GivenTheWarehouseSystemIsAvailable()
    {
        // DB is already initialised in [BeforeScenario]; nothing extra needed.
    }

    [Given(@"^the following articles exist in the warehouse:$")]
    public void GivenTheFollowingArticlesExistInTheWarehouse(DataTable dataTable)
    {
        using var ctx = CreateContext();
        foreach (var row in dataTable.Rows)
        {
            var articleNumber = long.Parse(row["ArticleNumber"]);
            var stock = int.Parse(row["Stock"]);
            var reserved = int.Parse(row["Reserved"]);

            var existing = ctx.ArticleStocks.FirstOrDefault(a => a.ArticleNumber == articleNumber);
            if (existing is null)
                ctx.ArticleStocks.Add(new ArticleStock(articleNumber, stock, reserved));
            else
            {
                existing.Stock = stock;
                existing.OfWhichReserved = reserved;
            }
        }
        ctx.SaveChanges();
    }

    [Given(@"^article (.*) has (.*) units in stock$")]
    public void GivenArticleHasUnitsInStock(long articleNumber, int stock)
        => EnsureArticle(articleNumber, stock, 0);

    [Given(@"^article (.*) has (.*) units in stock with (.*) reserved$")]
    public void GivenArticleHasUnitsInStockWithReserved(long articleNumber, int stock, int reserved)
        => EnsureArticle(articleNumber, stock, reserved);

    private void EnsureArticle(long articleNumber, int stock, int reserved)
    {
        using var ctx = CreateContext();
        var existing = ctx.ArticleStocks.FirstOrDefault(a => a.ArticleNumber == articleNumber);
        if (existing is null)
            ctx.ArticleStocks.Add(new ArticleStock(articleNumber, stock, reserved));
        else
        {
            existing.Stock = stock;
            existing.OfWhichReserved = reserved;
        }
        ctx.SaveChanges();
    }

    // ── When ────────────────────────────────────────────────────────────────

    [When(@"^I request the stock for articleNumber (.*)$")]
    public async Task WhenIRequestTheStockForArticleNumber(long articleNumber)
        => _lastResult = await CreateController().Stock(articleNumber);

    [When(@"^I add (.*) units to article (.*)$")]
    public async Task WhenIAddUnitsToArticle(int quantity, long articleNumber)
        => _lastResult = await CreateController().PlaceStockInWarehouse(
            new PlaceStockInWarehouseCommand { CorrelationId = Guid.NewGuid(), ArticleNumber = articleNumber, Quantity = quantity });

    [When(@"^I try to add (.*) units to article (.*)$")]
    public async Task WhenITryToAddUnitsToArticle(int quantity, long articleNumber)
        => _lastResult = await CreateController().PlaceStockInWarehouse(
            new PlaceStockInWarehouseCommand { CorrelationId = Guid.NewGuid(), ArticleNumber = articleNumber, Quantity = quantity });

    [When(@"^I remove (.*) units from article (.*)$")]
    public async Task WhenIRemoveUnitsFromArticle(int quantity, long articleNumber)
        => _lastResult = await CreateController().RetrieveStockFromWarehouse(
            new RetrieveStockFromWarehouseCommand { CorrelationId = Guid.NewGuid(), ArticleNumber = articleNumber, Quantity = quantity });

    [When(@"^I try to remove (.*) units from article (.*)$")]
    public async Task WhenITryToRemoveUnitsFromArticle(int quantity, long articleNumber)
        => _lastResult = await CreateController().RetrieveStockFromWarehouse(
            new RetrieveStockFromWarehouseCommand { CorrelationId = Guid.NewGuid(), ArticleNumber = articleNumber, Quantity = quantity });

    [When(@"^I reserve (.*) units for article (.*)$")]
    public async Task WhenIReserveUnitsForArticle(int quantity, long articleNumber)
        => _lastResult = await CreateController().ReserveStock(
            new ReserveStockCommand { CorrelationId = Guid.NewGuid(), ArticleNumber = articleNumber, Quantity = quantity });

    // Handles both "1 unit" and "5 units"
    [When(@"^I try to reserve (.*) units? for article (.*)$")]
    public async Task WhenITryToReserveUnitsForArticle(int quantity, long articleNumber)
        => _lastResult = await CreateController().ReserveStock(
            new ReserveStockCommand { CorrelationId = Guid.NewGuid(), ArticleNumber = articleNumber, Quantity = quantity });

    // ── Then ────────────────────────────────────────────────────────────────

    [Then(@"^the response status is (.*)$")]
    public void ThenTheResponseStatusIs(int expectedStatus)
    {
        var actual = _lastResult switch
        {
            ObjectResult r => r.StatusCode,
            StatusCodeResult r => (int?)r.StatusCode,
            _ => null
        };
        Assert.Equal(expectedStatus, actual);
    }

    [Then(@"^the response contains the stock information:$")]
    public void ThenTheResponseContainsStockInformation(DataTable dataTable)
    {
        var ok = Assert.IsType<OkObjectResult>(_lastResult);
        var result = Assert.IsType<WarehouseResult>(ok.Value);

        foreach (var row in dataTable.Rows)
        {
            Assert.Equal(long.Parse(row["ArticleNumber"]), result.ArticleNumber);
            Assert.Equal(int.Parse(row["Stock"]), result.NewStock);
            Assert.Equal(int.Parse(row["Reserved"]), result.OfWhichReserved);
        }
    }

    [Then(@"^the stock for article (.*) is (.*) units$")]
    public void ThenTheStockForArticleIsUnits(long articleNumber, int expectedStock)
    {
        var ok = Assert.IsType<OkObjectResult>(_lastResult);
        var result = Assert.IsType<WarehouseResult>(ok.Value);
        Assert.Equal(articleNumber, result.ArticleNumber);
        Assert.Equal(expectedStock, result.NewStock);
    }

    [Then(@"^the reserved stock for article (.*) is (.*) units$")]
    public void ThenTheReservedStockForArticleIsUnits(long articleNumber, int expectedReserved)
    {
        var ok = Assert.IsType<OkObjectResult>(_lastResult);
        var result = Assert.IsType<WarehouseResult>(ok.Value);
        Assert.Equal(articleNumber, result.ArticleNumber);
        Assert.Equal(expectedReserved, result.OfWhichReserved);
    }

    [Then(@"^the reserved stock for article (.*) remains (.*) units$")]
    public void ThenTheReservedStockForArticleRemainsUnits(long articleNumber, int expectedReserved)
    {
        using var ctx = CreateContext();
        var article = ctx.ArticleStocks.FirstOrDefault(a => a.ArticleNumber == articleNumber);
        Assert.NotNull(article);
        Assert.Equal(expectedReserved, article.OfWhichReserved);
    }

    [Then(@"^the error message indicates ""(.*)""$")]
    public void ThenTheErrorMessageIndicates(string expectedPhrase)
    {
        var actualMessage = _lastResult switch
        {
            ObjectResult { Value: Error err } => err.Message,
            ObjectResult { Value: Error[] errs } => string.Join("; ", errs.Select(e => e.Message)),
            _ => string.Empty
        };

        // Check that at least one significant word (length > 5) from the expected phrase
        // appears in the actual error message (case-insensitive).
        var keywords = expectedPhrase
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 5)
            .Select(w => w.ToLowerInvariant())
            .ToList();

        bool matched = keywords.Count > 0 &&
            keywords.Any(w => actualMessage.Contains(w, StringComparison.OrdinalIgnoreCase));

        Assert.True(matched,
            $"Error message '{actualMessage}' does not match expected phrase '{expectedPhrase}'");
    }

    [Then(@"^a ""(.*)"" event is published$")]
    public void ThenAnEventIsPublished(string eventTypeName)
    {
        bool found = _publishedEvents.Any(e => e.GetType().Name == eventTypeName);
        Assert.True(found,
            $"Expected event '{eventTypeName}' to be published. " +
            $"Published events: [{string.Join(", ", _publishedEvents.Select(e => e.GetType().Name))}]");
    }
}
