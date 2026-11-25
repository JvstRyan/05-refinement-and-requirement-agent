using Kantilever.WarehouseService.Commands;
using Kantilever.WarehouseService.DAL;
using Kantilever.WarehouseService.Events;
using Kantilever.WarehouseService.Exceptions;
using Kantilever.WarehouseService.Logic;
using Kantilever.WarehouseService.Model;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Moq;
using System;
using Thumper.Core;

namespace Kantilever.WarehouseService.Test.Logic
{
    [TestClass]
    public class WarehouseTest
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<WarehouseContext> _options;
        private readonly Mock<IThumperPublisher> _publisherMock;

        // [TestInitialize]
        public WarehouseTest()
        {
            _connection = new SqliteConnection("datasource=:memory:");
            _connection.Open();
            _options = new DbContextOptionsBuilder<WarehouseContext>()
                 .UseSqlite(_connection)
                 .Options;
            _publisherMock = new Mock<IThumperPublisher>();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _connection.Close();
        }

        #region FindArticleStockAsync
        [TestMethod]
        public async Task FindArticleStockAsync_GivesArticleStock()
        {
            // Arrange
            GivenaDatabaseWithOneArticle(articleNumber: 1, stock: 8, ofWhichReserved: 5);

            using var context = new WarehouseContext(_options);
            var sut = new Warehouse(context, _publisherMock.Object);

            // Act
            var result = await sut.FindArticleStockAsync(1);

            // Assert
            Assert.AreEqual(1, result.ArticleNumber);
            Assert.AreEqual(8, result.Stock);
            Assert.AreEqual(5, result.OfWhichReserved);
        }

        [TestMethod]
        public async Task FindArticleStockAsync_OfNonExistingArticle_throwsArticleNotFoundException()
        {
            // Arrange
            GivenaDatabaseWithOneArticle(articleNumber: 1, stock: 0);

            using var context = new WarehouseContext(_options);
            var sut = new Warehouse(context, _publisherMock.Object);

            // Act
            Func<Task> act = async () =>
            {
                await sut.FindArticleStockAsync(2);
            };

            // Assert
            var ex = await Assert.ThrowsExceptionAsync<ArticleNotFoundException>(act);
            Assert.AreEqual("The article with ArticleNumber 2 could not be found.", ex.Message);
        }
        #endregion FindArticleStockAsync

        #region PlaceStockInAsync
        [TestMethod]
        public async Task PlaceStockInAsync_VerhoogtStock()
        {
            // Arrange
            GivenaDatabaseWithOneArticle(articleNumber: 1, stock: 0);

            using var context = new WarehouseContext(_options);
            var sut = new Warehouse(context, _publisherMock.Object);

            // Act
            var command = new PlaceStockInWarehouseCommand
            {
                ArticleNumber = 1,
                Quantity = 5,
            };
            await sut.PlaceStockInAsync(command);

            // Assert
            AssertStock(articleNumber: 1, stock: 5);
        }

        [TestMethod]
        public async Task PlaceStockInAsync_ReturnsNewStock()
        {
            // Arrange
            GivenaDatabaseWithOneArticle(articleNumber: 1, stock: 3);

            using var context = new WarehouseContext(_options);
            var sut = new Warehouse(context, _publisherMock.Object);

            // Act
            var command = new PlaceStockInWarehouseCommand
            {
                ArticleNumber = 1,
                Quantity = 5,
            };
            ArticleStock result = await sut.PlaceStockInAsync(command);

            // Assert
            Assert.AreEqual(1, result.ArticleNumber);
            Assert.AreEqual(8, result.Stock);
        }

        [TestMethod]
        public async Task PlaceStockInAsyncTwice_VerhoogtStockTwice()
        {
            // Arrange
            GivenaDatabaseWithOneArticle(articleNumber: 1, stock: 0);

            using var context = new WarehouseContext(_options);
            var sut = new Warehouse(context, _publisherMock.Object);

            // Act
            var command1 = new PlaceStockInWarehouseCommand
            {
                ArticleNumber = 1,
                Quantity = 5,
            };
            await sut.PlaceStockInAsync(command1);
            var command2 = new PlaceStockInWarehouseCommand
            {
                ArticleNumber = 1,
                Quantity = 7,
            };
            await sut.PlaceStockInAsync(command2);

            // Assert
            AssertStock(articleNumber: 1, stock: 12);
        }

        [TestMethod]
        public async Task PlaceStockInAsync_PublishesCorrectEvent()
        {
            // Arrange
            GivenaDatabaseWithOneArticle(articleNumber: 1, stock: 3);

            using var context = new WarehouseContext(_options);
            var sut = new Warehouse(context, _publisherMock.Object);
            string? actualTopic = null;
            StockPlacedInWarehouse? actualEvent = null;
            _publisherMock.Setup(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<StockPlacedInWarehouse>()))
                          .Callback(
                                (string t, StockPlacedInWarehouse e) => { actualTopic = t; actualEvent = e; });

            // Act
            var guid = Guid.NewGuid();
            var command = new PlaceStockInWarehouseCommand
            {
                CorrelationId = guid,
                ArticleNumber = 1,
                Quantity = 5,
            };
            ArticleStock result = await sut.PlaceStockInAsync(command);

            // Assert
            Assert.AreEqual("Kantilever.WarehouseService.StockPlacedInWarehouse", actualTopic);
            Assert.IsNotNull(actualEvent);
            Assert.AreEqual(guid, actualEvent.CorrelationId);
            Assert.AreEqual(1, actualEvent.ArticleNumber);
            Assert.AreEqual(5, actualEvent.Quantity);
            Assert.AreEqual(8, actualEvent.NewStock);
        }

        [TestMethod]
        public async Task PlaceStockInAsync_OfNonExistingArticle_MaaktArtikelMetStock0()
        {
            // Arrange
            GivenaDatabaseWithOneArticle(articleNumber: 1, stock: 0);

            using var context = new WarehouseContext(_options);
            var sut = new Warehouse(context, _publisherMock.Object);

            // Act
            var command = new PlaceStockInWarehouseCommand
            {
                ArticleNumber = 2,
                Quantity = 5,
            };

            await sut.PlaceStockInAsync(command);

            // Assert
            AssertStock(articleNumber: 2, stock: 5, ofWhichReserved: 0);
        }
        #endregion PlaceStockInAsync

        #region RetrieveStockAsync
        [TestMethod]
        public async Task RetrieveStockAsync_VerlaagtStock()
        {
            // Arrange
            GivenaDatabaseWithOneArticle(articleNumber: 1, stock: 8);

            using var context = new WarehouseContext(_options);
            var sut = new Warehouse(context, _publisherMock.Object);

            // Act
            var command = new RetrieveStockFromWarehouseCommand
            {
                ArticleNumber = 1,
                Quantity = 5,
            };
            await sut.RetrieveStockAsync(command);

            // Assert
            AssertStock(articleNumber: 1, stock: 3);
        }

        [TestMethod]
        public async Task RetrieveStockAsync_ReturnsNewStock()
        {
            // Arrange
            GivenaDatabaseWithOneArticle(articleNumber: 1, stock: 8);

            using var context = new WarehouseContext(_options);
            var sut = new Warehouse(context, _publisherMock.Object);

            // Act
            var command = new RetrieveStockFromWarehouseCommand
            {
                ArticleNumber = 1,
                Quantity = 5,
            };
            ArticleStock result = await sut.RetrieveStockAsync(command);

            // Assert
            Assert.AreEqual(1, result.ArticleNumber);
            Assert.AreEqual(3, result.Stock);
        }

        [TestMethod]
        public async Task RetrieveStockAsync_PublishesCorrectEvent()
        {
            // Arrange
            GivenaDatabaseWithOneArticle(articleNumber: 1, stock: 8);

            using var context = new WarehouseContext(_options);
            var sut = new Warehouse(context, _publisherMock.Object);
            string? actualTopic = null;
            StockRetrievedFromWarehouse? actualEvent = null;
            _publisherMock.Setup(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<StockRetrievedFromWarehouse>()))
                          .Callback(
                                (string t, StockRetrievedFromWarehouse e) => { actualTopic = t; actualEvent = e; });

            // Act
            var guid = Guid.NewGuid();
            var command = new RetrieveStockFromWarehouseCommand
            {
                CorrelationId = guid,
                ArticleNumber = 1,
                Quantity = 5,
            };
            ArticleStock result = await sut.RetrieveStockAsync(command);

            // Assert
            Assert.AreEqual("Kantilever.WarehouseService.StockRetrievedFromWarehouse", actualTopic);
            Assert.IsNotNull(actualEvent);
            Assert.AreEqual(guid, actualEvent.CorrelationId);
            Assert.AreEqual(1, actualEvent.ArticleNumber);
            Assert.AreEqual(5, actualEvent.Quantity);
            Assert.AreEqual(3, actualEvent.NewStock);
        }

        [TestMethod]
        public async Task RetrieveStockAsync_OfNonExistingArticle_throwsArticleNotFoundException()
        {
            // Arrange
            GivenaDatabaseWithOneArticle(articleNumber: 1, stock: 0);

            using var context = new WarehouseContext(_options);
            var sut = new Warehouse(context, _publisherMock.Object);

            // Act
            var command = new RetrieveStockFromWarehouseCommand
            {
                ArticleNumber = 2,
                Quantity = 5,
            };
            Func<Task> act = async () =>
            {
                await sut.RetrieveStockAsync(command);
            };

            // Assert
            var ex = await Assert.ThrowsExceptionAsync<ArticleNotFoundException>(act);
            Assert.AreEqual("The article with ArticleNumber 2 could not be found.", ex.Message);
        }

        [TestMethod]
        public async Task RetrieveStockAsync_BijOnvoldoendeStock_throwsFunctionalException()
        {
            // Arrange
            GivenaDatabaseWithOneArticle(articleNumber: 1, stock: 2);

            using var context = new WarehouseContext(_options);
            var sut = new Warehouse(context, _publisherMock.Object);

            // Act
            var command = new RetrieveStockFromWarehouseCommand
            {
                ArticleNumber = 1,
                Quantity = 5,
            };
            Func<Task> act = async () =>
            {
                await sut.RetrieveStockAsync(command);
            };

            // Assert
            var ex = await Assert.ThrowsExceptionAsync<FunctionalException>(act);
            var error = ex.Errors.First();
            Assert.AreEqual("MAG002", error.Code);
            Assert.AreEqual("For article with ArticleNumber 1 the available stock of 2 is insufficient.", error.Message);
        }

        [TestMethod]
        public async Task RetrieveStockAsync_BijOnvoldoendeBeschikbareStock_throwsFunctionalException()
        {
            // Arrange
            GivenaDatabaseWithOneArticle(articleNumber: 1, stock: 6, ofWhichReserved: 2);

            using var context = new WarehouseContext(_options);
            var sut = new Warehouse(context, _publisherMock.Object);

            // Act
            var command = new RetrieveStockFromWarehouseCommand
            {
                ArticleNumber = 1,
                Quantity = 5,
            };
            Func<Task> act = async () =>
            {
                await sut.RetrieveStockAsync(command);
            };

            // Assert
            var ex = await Assert.ThrowsExceptionAsync<FunctionalException>(act);
            var error = ex.Errors.First();
            Assert.AreEqual("MAG002", error.Code);
            Assert.AreEqual("For article with ArticleNumber 1 the available stock of 4 is insufficient.", error.Message);
        }
        #endregion RetrieveStockAsync

        #region ReserveStockAsync
        [TestMethod]
        public async Task ReserveStockAsync_ReserveertStock()
        {
            // Arrange
            GivenaDatabaseWithOneArticle(articleNumber: 1, stock: 8, ofWhichReserved: 0);

            using var context = new WarehouseContext(_options);
            var sut = new Warehouse(context, _publisherMock.Object);

            // Act
            var command = new ReserveStockCommand
            {
                ArticleNumber = 1,
                Quantity = 5,
            };
            await sut.ReserveStockAsync(command);

            // Assert
            AssertStock(articleNumber: 1, stock: 8, ofWhichReserved: 5);
        }

        [TestMethod]
        public async Task ReserveStock_ReturnsNewStock()
        {
            // Arrange
            GivenaDatabaseWithOneArticle(articleNumber: 1, stock: 8, ofWhichReserved: 0);

            using var context = new WarehouseContext(_options);
            var sut = new Warehouse(context, _publisherMock.Object);

            // Act
            var command = new ReserveStockCommand
            {
                ArticleNumber = 1,
                Quantity = 5,
            };
            ArticleStock result = await sut.ReserveStockAsync(command);

            // Assert
            Assert.AreEqual(1, result.ArticleNumber);
            Assert.AreEqual(8, result.Stock);
            Assert.AreEqual(5, result.OfWhichReserved);
        }

        [TestMethod]
        public async Task ReserveStockAsyncTwice_ReserveertMeerStock()
        {
            // Arrange
            GivenaDatabaseWithOneArticle(articleNumber: 1, stock: 20, ofWhichReserved: 2);

            using var context = new WarehouseContext(_options);
            var sut = new Warehouse(context, _publisherMock.Object);

            // Act
            var command1 = new ReserveStockCommand
            {
                ArticleNumber = 1,
                Quantity = 5,
            };
            await sut.ReserveStockAsync(command1);
            var command2 = new ReserveStockCommand
            {
                ArticleNumber = 1,
                Quantity = 7,
            };
            await sut.ReserveStockAsync(command2);

            // Assert
            AssertStock(articleNumber: 1, stock: 20, ofWhichReserved: 14);
        }

        [TestMethod]
        public async Task ReserveStock_PublishesCorrectEvent()
        {
            // Arrange
            GivenaDatabaseWithOneArticle(articleNumber: 1, stock: 8, ofWhichReserved: 1);

            using var context = new WarehouseContext(_options);
            var sut = new Warehouse(context, _publisherMock.Object);
            string? actualTopic = null;
            StockReserved? actualEvent = null;
            _publisherMock.Setup(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<StockReserved>()))
                          .Callback(
                                (string t, StockReserved e) => { actualTopic = t; actualEvent = e; });
            // Act
            var guid = Guid.NewGuid();
            var command = new ReserveStockCommand
            {
                CorrelationId = guid,
                ArticleNumber = 1,
                Quantity = 5,
            };
            ArticleStock result = await sut.ReserveStockAsync(command);

            // Assert
            Assert.AreEqual("Kantilever.WarehouseService.StockReserved", actualTopic);
            Assert.IsNotNull(actualEvent);
            Assert.AreEqual(guid, actualEvent.CorrelationId);
            Assert.AreEqual(1, actualEvent.ArticleNumber);
            Assert.AreEqual(5, actualEvent.Quantity);
            Assert.AreEqual(6, actualEvent.NewReservedQuantity);
        }

        [TestMethod]
        public async Task ReserveStockAsync_OfNonExistingArticle_throwsArticleNotFoundException()
        {
            // Arrange
            GivenaDatabaseWithOneArticle(articleNumber: 1, stock: 0);

            using var context = new WarehouseContext(_options);
            var sut = new Warehouse(context, _publisherMock.Object);

            // Act
            var command = new ReserveStockCommand
            {
                ArticleNumber = 2,
                Quantity = 5,
            };
            Func<Task> act = async () =>
            {
                await sut.ReserveStockAsync(command);
            };

            // Assert
            var ex = await Assert.ThrowsExceptionAsync<ArticleNotFoundException>(act);
            Assert.AreEqual("The article with ArticleNumber 2 could not be found.", ex.Message);
        }

        [TestMethod]
        public async Task ReserveStockAsync_BijOnvoldoendeStock_throwsFunctionalException()
        {
            // Arrange
            GivenaDatabaseWithOneArticle(articleNumber: 1, stock: 3, ofWhichReserved: 0);

            using var context = new WarehouseContext(_options);
            var sut = new Warehouse(context, _publisherMock.Object);

            // Act
            var command = new ReserveStockCommand
            {
                ArticleNumber = 1,
                Quantity = 5,
            };
            Func<Task> act = async () =>
            {
                await sut.ReserveStockAsync(command);
            };

            // Assert
            var ex = await Assert.ThrowsExceptionAsync<FunctionalException>(act);
            var error = ex.Errors.First();
            Assert.AreEqual("MAG002", error.Code);
            Assert.AreEqual("For article with ArticleNumber 1 the available stock of 3 is insufficient.", error.Message);
        }

        [TestMethod]
        public async Task ReserveStockAsync_BijOnvoldoendeBeschikbareStock_throwsFunctionalException()
        {
            // Arrange
            GivenaDatabaseWithOneArticle(articleNumber: 1, stock: 6, ofWhichReserved: 2);

            using var context = new WarehouseContext(_options);
            var sut = new Warehouse(context, _publisherMock.Object);

            // Act
            var command = new ReserveStockCommand
            {
                ArticleNumber = 1,
                Quantity = 5,
            };
            Func<Task> act = async () =>
            {
                await sut.ReserveStockAsync(command);
            };

            // Assert
            var ex = await Assert.ThrowsExceptionAsync<FunctionalException>(act);
            var error = ex.Errors.First();
            Assert.AreEqual("MAG002", error.Code);
            Assert.AreEqual("For article with ArticleNumber 1 the available stock of 4 is insufficient.", error.Message);
        }
        #endregion ReserveStockAsync


        #region ReserveStockEvenIfUnavailableAsync
        [TestMethod]
        public async Task ReserveStockEvenIfUnavailableAsync_BijOnvoldoendeStock_ZorgtVoorMeerReserveringDanErStockIs()
        {
            // Arrange
            GivenaDatabaseWithOneArticle(articleNumber: 1, stock: 3, ofWhichReserved: 0);

            using var context = new WarehouseContext(_options);
            var sut = new Warehouse(context, _publisherMock.Object);

            // Act
            var command = new ReserveStockEvenIfUnavailableCommand
            {
                ArticleNumber = 1,
                Quantity = 5,
            };
            await sut.ReserveStockEvenIfUnavailableAsync(command);

            // Assert
            AssertStock(articleNumber: 1, stock: 3, ofWhichReserved: 5);
        }
        #endregion ReserveStockEvenIfUnavailableAsync


        #region RetrieveReservedStockAsync
        [TestMethod]
        public async Task RetrieveReservedStockAsync_VerlaagtStockEnReservering()
        {
            // Arrange
            GivenaDatabaseWithOneArticle(articleNumber: 1, stock: 8, ofWhichReserved: 6);

            using var context = new WarehouseContext(_options);
            var sut = new Warehouse(context, _publisherMock.Object);

            // Act
            var command = new RetrieveReservedStockFromWarehouseCommand
            {
                ArticleNumber = 1,
                Quantity = 5,
            };
            await sut.RetrieveReservedStockAsync(command);

            // Assert
            AssertStock(articleNumber: 1, stock: 3, ofWhichReserved: 1);
        }

        [TestMethod]
        public async Task RetrieveReservedStockAsync_ReturnsNewStock()
        {
            // Arrange
            GivenaDatabaseWithOneArticle(articleNumber: 1, stock: 8, ofWhichReserved: 6);

            using var context = new WarehouseContext(_options);
            var sut = new Warehouse(context, _publisherMock.Object);

            // Act
            var command = new RetrieveReservedStockFromWarehouseCommand
            {
                ArticleNumber = 1,
                Quantity = 5,
            };
            ArticleStock result = await sut.RetrieveReservedStockAsync(command);

            // Assert
            Assert.AreEqual(1, result.ArticleNumber);
            Assert.AreEqual(3, result.Stock);
            Assert.AreEqual(1, result.OfWhichReserved);
        }

        [TestMethod]
        public async Task RetrieveReservedStockAsync_PublishesCorrectEvent()
        {
            // Arrange
            GivenaDatabaseWithOneArticle(articleNumber: 1, stock: 8, ofWhichReserved: 6);

            using var context = new WarehouseContext(_options);
            var sut = new Warehouse(context, _publisherMock.Object);
            string? actualTopic = null;
            ReservedStockRetrievedFromWarehouse? actualEvent = null;
            _publisherMock.Setup(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<ReservedStockRetrievedFromWarehouse>()))
                          .Callback(
                                (string t, ReservedStockRetrievedFromWarehouse e) => { actualTopic = t; actualEvent = e; });

            // Act
            var guid = Guid.NewGuid();
            var command = new RetrieveReservedStockFromWarehouseCommand
            {
                CorrelationId = guid,
                ArticleNumber = 1,
                Quantity = 5,
            };
            ArticleStock result = await sut.RetrieveReservedStockAsync(command);

            // Assert
            Assert.AreEqual("Kantilever.WarehouseService.ReservedStockRetrievedFromWarehouse", actualTopic);
            Assert.IsNotNull(actualEvent);
            Assert.AreEqual(guid, actualEvent.CorrelationId);
            Assert.AreEqual(1, actualEvent.ArticleNumber);
            Assert.AreEqual(5, actualEvent.Quantity);
            Assert.AreEqual(3, actualEvent.NewStock);
            Assert.AreEqual(1, actualEvent.NewReservedQuantity);
        }

        [TestMethod]
        public async Task RetrieveReservedStockAsync_OfNonExistingArticle_throwsArticleNotFoundException()
        {
            // Arrange
            GivenaDatabaseWithOneArticle(articleNumber: 1, stock: 0);

            using var context = new WarehouseContext(_options);
            var sut = new Warehouse(context, _publisherMock.Object);

            // Act
            var command = new RetrieveReservedStockFromWarehouseCommand
            {
                ArticleNumber = 2,
                Quantity = 5,
            };
            Func<Task> act = async () =>
            {
                await sut.RetrieveReservedStockAsync(command);
            };

            // Assert
            var ex = await Assert.ThrowsExceptionAsync<ArticleNotFoundException>(act);
            Assert.AreEqual("The article with ArticleNumber 2 could not be found.", ex.Message);
        }

        [TestMethod]
        public async Task RetrieveReservedStockAsync_BijOnvoldoendeGereserveerdeStock_throwsFunctionalException()
        {
            // Arrange
            GivenaDatabaseWithOneArticle(articleNumber: 1, stock: 8, ofWhichReserved: 4);

            using var context = new WarehouseContext(_options);
            var sut = new Warehouse(context, _publisherMock.Object);

            // Act
            var command = new RetrieveReservedStockFromWarehouseCommand
            {
                ArticleNumber = 1,
                Quantity = 5,
            };
            Func<Task> act = async () =>
            {
                await sut.RetrieveReservedStockAsync(command);
            };

            // Assert
            var ex = await Assert.ThrowsExceptionAsync<FunctionalException>(act);
            var error = ex.Errors.First();
            Assert.AreEqual("MAG004", error.Code);
            Assert.AreEqual("For article with ArticleNumber 1 the reserved stock of 4 is insufficient.", error.Message);
       }
        #endregion RetrieveReservedStockAsync

        #region ReleaseReservedStockAsync
        [TestMethod]
        public async Task ReleaseReservedStockAsync_VerlaagtReservering()
        {
            // Arrange
            GivenaDatabaseWithOneArticle(articleNumber: 1, stock: 8, ofWhichReserved: 6);

            using var context = new WarehouseContext(_options);
            var sut = new Warehouse(context, _publisherMock.Object);

            // Act
            var command = new ReleaseReservedStockCommand
            {
                ArticleNumber = 1,
                Quantity = 5,
            };
            await sut.ReleaseReservedStockAsync(command);

            // Assert
            AssertStock(articleNumber: 1, stock: 8, ofWhichReserved: 1);
        }

        [TestMethod]
        public async Task ReleaseReservedStockAsync_ReturnsNewStock()
        {
            // Arrange
            GivenaDatabaseWithOneArticle(articleNumber: 1, stock: 8, ofWhichReserved: 8);

            using var context = new WarehouseContext(_options);
            var sut = new Warehouse(context, _publisherMock.Object);

            // Act
            var command = new ReleaseReservedStockCommand
            {
                ArticleNumber = 1,
                Quantity = 5,
            };
            ArticleStock result = await sut.ReleaseReservedStockAsync(command);

            // Assert
            Assert.AreEqual(1, result.ArticleNumber);
            Assert.AreEqual(8, result.Stock);
            Assert.AreEqual(3, result.OfWhichReserved);
        }

        [TestMethod]
        public async Task ReleaseReservedStockAsync_PublishesCorrectEvent()
        {
            // Arrange
            GivenaDatabaseWithOneArticle(articleNumber: 1, stock: 8, ofWhichReserved: 8);

            using var context = new WarehouseContext(_options);
            var sut = new Warehouse(context, _publisherMock.Object);
            string? actualTopic = null;
            ReservedStockReleased? actualEvent = null;
            _publisherMock.Setup(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<ReservedStockReleased>()))
                          .Callback(
                                (string t, ReservedStockReleased e) => { actualTopic = t; actualEvent = e; });
            // Act
            var guid = Guid.NewGuid();
            var command = new ReleaseReservedStockCommand
            {
                CorrelationId = guid,
                ArticleNumber = 1,
                Quantity = 3,
            };
            ArticleStock result = await sut.ReleaseReservedStockAsync(command);

            // Assert
            Assert.AreEqual("Kantilever.WarehouseService.ReservedStockReleased", actualTopic);
            Assert.IsNotNull(actualEvent);
            Assert.AreEqual(guid, actualEvent.CorrelationId);
            Assert.AreEqual(1, actualEvent.ArticleNumber);
            Assert.AreEqual(3, actualEvent.Quantity);
            Assert.AreEqual(5, actualEvent.NewReservedQuantity);
        }

        [TestMethod]
        public async Task ReleaseReservedStockAsync_OfNonExistingArticle_throwsArticleNotFoundException()
        {
            // Arrange
            GivenaDatabaseWithOneArticle(articleNumber: 1, stock: 0);

            using var context = new WarehouseContext(_options);
            var sut = new Warehouse(context, _publisherMock.Object);

            // Act
            var command = new ReleaseReservedStockCommand
            {
                ArticleNumber = 2,
                Quantity = 5,
            };
            Func<Task> act = async () =>
            {
                await sut.ReleaseReservedStockAsync(command);
            };

            // Assert
            var ex = await Assert.ThrowsExceptionAsync<ArticleNotFoundException>(act);
            Assert.AreEqual("The article with ArticleNumber 2 could not be found.", ex.Message);
        }

        [TestMethod]
        public async Task ReleaseReservedStockAsync_BijOnvoldoendeGereserveerdeStock_throwsFunctionalException()
        {
            // Arrange
            GivenaDatabaseWithOneArticle(articleNumber: 1, stock: 8, ofWhichReserved: 4);

            using var context = new WarehouseContext(_options);
            var sut = new Warehouse(context, _publisherMock.Object);

            // Act
            var command = new ReleaseReservedStockCommand
            {
                ArticleNumber = 1,
                Quantity = 5,
            };
            Func<Task> act = async () =>
            {
                await sut.ReleaseReservedStockAsync(command);
            };

            // Assert
            var ex = await Assert.ThrowsExceptionAsync<FunctionalException>(act);
            var error = ex.Errors.First();
            Assert.AreEqual("MAG004", error.Code);
            Assert.AreEqual("For article with ArticleNumber 1 the reserved stock of 4 is insufficient.", error.Message);
         }
        #endregion ReleaseReservedStockAsync


        private void GivenaDatabaseWithOneArticle(long articleNumber, int stock, int ofWhichReserved = 0)
        {
            using (var context = new WarehouseContext(_options))
            {
                context.Database.EnsureCreated();
                context.ArticleStocks.Add(new(articleNumber, stock, ofWhichReserved));
                context.SaveChanges();
            }
        }

        private void AssertStock(long articleNumber, int stock, int ofWhichReserved = 0)
        {
            using (var assertcontext = new WarehouseContext(_options))
            {
                var articleStock = assertcontext.ArticleStocks
                    .SingleOrDefault(av => av.ArticleNumber == articleNumber);

                Assert.IsNotNull(articleStock);
                Assert.AreEqual(articleNumber, articleStock.ArticleNumber);
                Assert.AreEqual(stock, articleStock.Stock);
                Assert.AreEqual(ofWhichReserved, articleStock.OfWhichReserved);
            }
        }
    }
}