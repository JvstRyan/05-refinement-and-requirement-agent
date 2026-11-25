using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Kantilever.WarehouseService.Commands;
using Kantilever.WarehouseService.DAL;
using Kantilever.WarehouseService.Logic;
using Kantilever.WarehouseService.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Polly.Retry;
using Polly;
using System.Net.Sockets;
using System.Xml.Linq;
using Microsoft.Extensions.DependencyInjection;
using Kantilever.WarehouseService.Controllers;
using Thumper.Core;
using System.Text.Json;
using Kantilever.WarehouseService.Events;

namespace Kantilever.WarehouseService.IntegrationTest
{
    [TestClass]
    public class WarehouseTest
    {
        private IContainer? _postgresContainer;
        private IContainer? _rabbitmqContainer;
        private ServiceProvider _serviceProvider;
        private IBasicReceiver _receiver;
        private EventMessage _actualEventMessage;
        private AutoResetEvent _eventReceiveFlag;

        [TestInitialize] 
        public async Task Initialize()
        {
            _postgresContainer = new ContainerBuilder()
                .WithImage("postgres:17")
                .WithCleanUp(true)
                .WithAutoRemove(true)
                .WithPortBinding(5432, true)
                .WithEnvironment("POSTGRES_PASSWORD", "Geheim_101")
                .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
                .Build();
            _rabbitmqContainer = new ContainerBuilder()
                .WithImage("rabbitmq")
                .WithCleanUp(true)
                .WithAutoRemove(true)
                .WithPortBinding(5672, true)
                .WithEnvironment("RABBITMQ_DEFAULT_USER", "Kantilever")
                .WithEnvironment("RABBITMQ_DEFAULT_PASS", "Kant1lever")
                .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5672))
                .Build();

            await Task.WhenAll(
                _postgresContainer.StartAsync(),
                _rabbitmqContainer.StartAsync()
            );

            var host = _postgresContainer.Hostname;
            var port = _postgresContainer.GetMappedPublicPort(5432);
            string? connectionString =
                $"Host={host};Port={port};Database=WarehouseDB;Username=postgres;Password=Geheim_101";

            var thumperOptions = new ThumperOptions
            {
                HostName = _rabbitmqContainer.Hostname,
                Port = _rabbitmqContainer.GetMappedPublicPort(5672),
                UserName = "Kantilever",
                Password = "Kant1lever",
                ExchangeName = "Kantilever.Eventbus",
                QueueName = "IntegrationTestQueue"
            };
            var thumperContext = new ThumperContext(thumperOptions);

            _receiver = new BasicReceiver(thumperContext);

            _serviceProvider = new ServiceCollection()
                .AddWarehouseDependencies(connectionString, thumperOptions)
                .BuildServiceProvider();
        }

        [TestCleanup]
        public async Task Cleanup()
        {
            _receiver?.Dispose();

            await Task.WhenAll(
                _postgresContainer!.StopAsync(),
                _rabbitmqContainer!.StopAsync()
            );
        }

        [TestMethod]
        public async Task PlaceStockInWarehouse()
        {
            string topic = "Kantilever.WarehouseService.StockPlacedInWarehouse";
            Given_AnArticleWith(articleNumber: 1, stock: 5, ofWhichReserved: 2);
            Given_anEventListener(topic: topic);

            await When_StockIsAdded(articleNumber: 1, quantity: 6);

            Then_DeStockIs(articleNumber: 1, stock: 11, ofWhichReserved: 2);
            Then_EventWithCorrectContentWasPublished(
                topic: topic, articleNumber: 1, quantity: 6, newStock: 11);
        }

        [TestMethod]
        public async Task RetrieveReservedStockFromWarehouse()
        {
            string topic = "Kantilever.WarehouseService.ReservedStockRetrievedFromWarehouse";
            Given_AnArticleWith(articleNumber: 1, stock: 7, ofWhichReserved: 4);
            await Given_anEventListener(topic: topic);

            await When_ReservedStockIsRetrieved(articleNumber: 1, quantity: 3);

            Then_DeStockIs(articleNumber: 1, stock: 4, ofWhichReserved: 1);
            Then_EventWithCorrectContentWasPublished(
                topic: topic, articleNumber: 1, quantity: 3, newStock: 4, nieuwQuantityGereserveerd : 1);
        }


        private void Given_AnArticleWith(long articleNumber, int stock, int ofWhichReserved)
        {
            using (var scope = _serviceProvider.CreateScope())
            using (var context = scope.ServiceProvider.GetRequiredService<WarehouseContext>())
            {
                context.Database.EnsureCreated();
                context.ArticleStocks.Add(new(articleNumber, stock, ofWhichReserved));
                context.SaveChanges();
            }
        }

        private async Task Given_anEventListener(string topic)
        {
            _actualEventMessage = null!;
            _eventReceiveFlag = new AutoResetEvent(false);

            await _receiver.SetupQueueAsync(new string[] { topic });
            await _receiver.StartReceivingAsync(async(em) => { _actualEventMessage = em; _eventReceiveFlag.Set(); });
        }


        private async Task When_StockIsAdded(long articleNumber, int quantity)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var warehouse = scope.ServiceProvider.GetRequiredService<IWarehouse>();
                var sut = new WarehouseController(warehouse);

                // Act
                var command = new PlaceStockInWarehouseCommand
                {
                    CorrelationId = Guid.NewGuid(),
                    ArticleNumber = articleNumber,
                    Quantity = quantity,
                };
                await sut.PlaceStockInWarehouse(command);
            }
        }

        private async Task When_ReservedStockIsRetrieved(long articleNumber, int quantity)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var warehouse = scope.ServiceProvider.GetRequiredService<IWarehouse>();
                var sut = new WarehouseController(warehouse);

                // Act
                var command = new RetrieveReservedStockFromWarehouseCommand
                {
                    CorrelationId = Guid.NewGuid(),
                    ArticleNumber = articleNumber,
                    Quantity = quantity,
                };
                await sut.RetrieveReservedStockFromWarehouse(command);
            }
        }


        private void Then_DeStockIs(long articleNumber, int stock, int ofWhichReserved)
        {
            using (var scope = _serviceProvider.CreateScope())
            using (var context = scope.ServiceProvider.GetRequiredService<WarehouseContext>())
            {
                var articleStock = context.ArticleStocks
                    .SingleOrDefault(av => av.ArticleNumber == articleNumber);

                Assert.IsNotNull(articleStock);
                Assert.AreEqual(articleNumber, articleStock.ArticleNumber, $"articleNumber should be {articleNumber}.");
                Assert.AreEqual(stock, articleStock.Stock, $"stock should be {stock}.");
                Assert.AreEqual(ofWhichReserved, articleStock.OfWhichReserved, $"gereserveerde stock should be {ofWhichReserved}.");
            }
        }

        private void Then_EventWithCorrectContentWasPublished(string topic, long articleNumber, int quantity, int newStock = 0, int nieuwQuantityGereserveerd = 0)
        {
            bool eventReceived = _eventReceiveFlag.WaitOne(TimeSpan.FromSeconds(2));
            Assert.IsTrue(eventReceived, "An event should have been received.");

            Assert.AreEqual(topic, _actualEventMessage.Topic, $"topic should be '{topic}'.");

            var evt = JsonSerializer.Deserialize<EventData>(_actualEventMessage.Body);
            Assert.IsNotNull(evt, "data should be able to deserialize");

            Assert.AreEqual(articleNumber, evt.ArticleNumber, $"articleNumber should be {articleNumber}.");
            Assert.AreEqual(quantity, evt.Quantity, $"quantity should be {quantity}.");
            Assert.AreEqual(newStock, evt.NewStock, $"nieuwe stock should be {newStock}.");
            Assert.AreEqual(nieuwQuantityGereserveerd, evt.NewReservedQuantity, $"nieuwe quantity gereserveerd should be {nieuwQuantityGereserveerd}.");
        }
    }
}