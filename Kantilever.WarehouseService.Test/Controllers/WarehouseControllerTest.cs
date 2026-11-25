using Kantilever.WarehouseService.Commands;
using Kantilever.WarehouseService.Controllers;
using Kantilever.WarehouseService.Exceptions;
using Kantilever.WarehouseService.Logic;
using Kantilever.WarehouseService.Model;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kantilever.WarehouseService.Test.Controllers
{
    [TestClass]
    public class WarehouseControllerTest
    {
        #region Stock
        [TestMethod]
        public async Task Stock_ReturnsArticleStock()
        {
            var mock = new Mock<IWarehouse>(MockBehavior.Strict);
            mock.Setup(mag => mag.FindArticleStockAsync(It.IsAny<long>()))
                .Returns(Task.FromResult(new ArticleStock
                {
                    ArticleNumber = 1301,
                    Stock = 11,
                    OfWhichReserved = 3,
                }));

            var sut = new WarehouseController(mock.Object);

            // Act
            var result = await sut.Stock(articleNumber: 1301);

            // Assert
            Assert.IsInstanceOfType(result, typeof(OkObjectResult));
            var okObjectResult = (OkObjectResult)result;
            Assert.IsInstanceOfType(okObjectResult.Value, typeof(WarehouseResult));
            WarehouseResult warehouseResult = (WarehouseResult)okObjectResult.Value!;

            Assert.AreEqual(1301, warehouseResult.ArticleNumber);
            Assert.AreEqual(11, warehouseResult.NewStock);
            Assert.AreEqual(3, warehouseResult.OfWhichReserved);
        }

        [TestMethod]
        public async Task Stock_returns404_WhenArticleNotFound()
        {
            var mock = new Mock<IWarehouse>(MockBehavior.Strict);
            mock.Setup(mag => mag.FindArticleStockAsync(It.IsAny<long>()))
                .ThrowsAsync(new ArticleNotFoundException(articleNumber: 1107));

            var sut = new WarehouseController(mock.Object);
            var result = await sut.Stock(1107);

            Assert.IsInstanceOfType(result, typeof(NotFoundObjectResult));
            var notFoundObjectResult = (NotFoundObjectResult)result;
            Assert.IsInstanceOfType(notFoundObjectResult.Value, typeof(Error));
            var error = (Error)notFoundObjectResult.Value!;
            Assert.AreEqual("MAG001", error.Code);
            Assert.AreEqual("The article with ArticleNumber 1107 could not be found.", error.Message);
        }
        #endregion Stock

        #region PlaceStockInWarehouse
        [TestMethod]
        public async Task PlaceStockInWarehouse_echoes_CorrelationId()
        {
            var mock = new Mock<IWarehouse>(MockBehavior.Strict);
            mock.Setup(mag => mag.PlaceStockInAsync(It.IsAny<PlaceStockInWarehouseCommand>()))
                .Returns(Task.FromResult(new ArticleStock()));

            var sut = new WarehouseController(mock.Object);

            var correlationId = Guid.NewGuid();
            var command = new PlaceStockInWarehouseCommand
            {
                CorrelationId = correlationId,
                ArticleNumber = 1106,
                Quantity = 6,
            };

            var result = await sut.PlaceStockInWarehouse(command);

            Assert.IsInstanceOfType(result, typeof(OkObjectResult));
            var okObjectResult = (OkObjectResult)result;
            Assert.IsInstanceOfType(okObjectResult.Value, typeof(WarehouseResult));
            WarehouseResult warehouseResult = (WarehouseResult)okObjectResult.Value!;
            Assert.AreEqual(correlationId, warehouseResult?.CorrelationId);
        }

        [TestMethod]
        public async Task PlaceStockInWarehouse_ReturnsNewStock()
        {
            var mock = new Mock<IWarehouse>(MockBehavior.Strict);
            mock.Setup(mag => mag.PlaceStockInAsync(It.IsAny<PlaceStockInWarehouseCommand>()))
                .Returns(Task.FromResult(new ArticleStock
                {
                    ArticleNumber = 1301,
                    Stock = 11,
                    OfWhichReserved = 3,
                }));

            var sut = new WarehouseController(mock.Object);

            // Act
            var command = new PlaceStockInWarehouseCommand { Quantity = 1 };
            var result = await sut.PlaceStockInWarehouse(command);

            // Assert
            Assert.IsInstanceOfType(result, typeof(OkObjectResult));
            var okObjectResult = (OkObjectResult)result;
            Assert.IsInstanceOfType(okObjectResult.Value, typeof(WarehouseResult));
            WarehouseResult warehouseResult = (WarehouseResult)okObjectResult.Value!;

            Assert.AreEqual(1301, warehouseResult.ArticleNumber);
            Assert.AreEqual(11, warehouseResult.NewStock);
            Assert.AreEqual(3, warehouseResult.OfWhichReserved);
        }

        [TestMethod]
        public async Task PlaceStockInWarehouse_WithNegativeQuantity_returnsError()
        {
            var mock = new Mock<IWarehouse>(MockBehavior.Strict);
            var sut = new WarehouseController(mock.Object);

            var command = new PlaceStockInWarehouseCommand
            {
                ArticleNumber = 1105,
                Quantity = -1,
            };

            var result = await sut.PlaceStockInWarehouse(command);

            Assert.IsInstanceOfType(result, typeof(UnprocessableEntityObjectResult));
            var unprocessableEntityObjectResult = (UnprocessableEntityObjectResult)result;
            Assert.IsInstanceOfType(unprocessableEntityObjectResult.Value, typeof(Error));
            var error = (Error)unprocessableEntityObjectResult.Value!;
            Assert.AreEqual("MAG003", error.Code);
            Assert.AreEqual("The quantity (-1) cannot be negative.", error.Message);
        }
        #endregion PlaceStockInWarehouse

        #region RetrieveStockFromWarehouse
        [TestMethod]
        public async Task RetrieveStockFromWarehouse_echoes_CorrelationId()
        {
            var mock = new Mock<IWarehouse>(MockBehavior.Strict);
            mock.Setup(mag => mag.RetrieveStockAsync(It.IsAny<RetrieveStockFromWarehouseCommand>()))
                .Returns(Task.FromResult(new ArticleStock()));

            var sut = new WarehouseController(mock.Object);

            var correlationId = Guid.NewGuid();
            var command = new RetrieveStockFromWarehouseCommand
            {
                CorrelationId = correlationId,
                ArticleNumber = 1109,
                Quantity = 9,
            };

            var result = await sut.RetrieveStockFromWarehouse(command);

            Assert.IsInstanceOfType(result, typeof(OkObjectResult));
            var okObjectResult = (OkObjectResult)result;
            Assert.IsInstanceOfType(okObjectResult.Value, typeof(WarehouseResult));
            var warehouseResult = (WarehouseResult)okObjectResult.Value!;
            Assert.AreEqual(correlationId, warehouseResult.CorrelationId);
        }

        [TestMethod]
        public async Task RetrieveStockFromWarehouse_ReturnsNewStock()
        {
            var mock = new Mock<IWarehouse>(MockBehavior.Strict);
            mock.Setup(mag => mag.RetrieveStockAsync(It.IsAny<RetrieveStockFromWarehouseCommand>()))
                .Returns(Task.FromResult(new ArticleStock
                {
                    ArticleNumber = 1301,
                    Stock = 11,
                    OfWhichReserved = 5,
                }));
            var sut = new WarehouseController(mock.Object);

            var command = new RetrieveStockFromWarehouseCommand { Quantity = 1 };
            var result = await sut.RetrieveStockFromWarehouse(command);

            Assert.IsInstanceOfType(result, typeof(OkObjectResult));
            var okObjectResult = (OkObjectResult)result;
            Assert.IsInstanceOfType(okObjectResult.Value, typeof(WarehouseResult));
            var warehouseResult = (WarehouseResult)okObjectResult.Value!;

            Assert.AreEqual(1301, warehouseResult.ArticleNumber);
            Assert.AreEqual(11, warehouseResult.NewStock);
            Assert.AreEqual(5, warehouseResult.OfWhichReserved);
        }

        [TestMethod]
        public async Task RetrieveStockFromWarehouse_returns404_WhenArticleNotFound()
        {
            var mock = new Mock<IWarehouse>(MockBehavior.Strict);
            mock.Setup(mag => mag.RetrieveStockAsync(It.IsAny<RetrieveStockFromWarehouseCommand>()))
                .ThrowsAsync(new ArticleNotFoundException(articleNumber: 1110));

            var sut = new WarehouseController(mock.Object);
            var command = new RetrieveStockFromWarehouseCommand
            {
                CorrelationId = Guid.NewGuid(),
                ArticleNumber = 1110,
                Quantity = 10,
            };
            var result = await sut.RetrieveStockFromWarehouse(command);

            Assert.IsInstanceOfType(result, typeof(NotFoundObjectResult));
            var notFoundObjectResult = (NotFoundObjectResult)result;
            Assert.IsInstanceOfType(notFoundObjectResult.Value, typeof(Error));
            var error = (Error)notFoundObjectResult.Value!;
            Assert.AreEqual("MAG001", error.Code);
            Assert.AreEqual("The article with ArticleNumber 1110 could not be found.", error.Message);
        }

        [TestMethod]
        public async Task RetrieveStockFromWarehouse_returns409_WhenStockIsInsufficient()
        {
            var mock = new Mock<IWarehouse>(MockBehavior.Strict);
            mock.Setup(mag => mag.RetrieveStockAsync(It.IsAny<RetrieveStockFromWarehouseCommand>()))
                .ThrowsAsync(new FunctionalException(new List<Error>() {
                    Errors.InsufficientStock(articleNumber: 1201, availableStock: 1)
                    }));

            var sut = new WarehouseController(mock.Object);
            var command = new RetrieveStockFromWarehouseCommand
            {
                CorrelationId = Guid.NewGuid(),
                ArticleNumber = 1201,
                Quantity = 4,
            };
            var result = await sut.RetrieveStockFromWarehouse(command);

            Assert.IsInstanceOfType(result, typeof(ConflictObjectResult));
            var conflictObjectResult = (ConflictObjectResult)result;
            Assert.IsInstanceOfType(conflictObjectResult.Value, typeof(Error[]));
            var errors = (Error[])conflictObjectResult.Value!;
            Assert.AreEqual(1, errors.Length);
            Assert.AreEqual("MAG002", errors[0].Code);
        }

        [TestMethod]
        public async Task RetrieveStockFromWarehouse_WithNegativeQuantity_returnsError()
        {
            var mock = new Mock<IWarehouse>(MockBehavior.Strict);
            var sut = new WarehouseController(mock.Object);

            var command = new RetrieveStockFromWarehouseCommand
            {
                ArticleNumber = 1105,
                Quantity = -1,
            };

            var result = await sut.RetrieveStockFromWarehouse(command);

            Assert.IsInstanceOfType(result, typeof(UnprocessableEntityObjectResult));
            var unprocessableEntityObjectResult = (UnprocessableEntityObjectResult)result;
            Assert.IsInstanceOfType(unprocessableEntityObjectResult.Value, typeof(Error));
            var error = (Error)unprocessableEntityObjectResult.Value!;
            Assert.AreEqual("MAG003", error.Code);
            Assert.AreEqual("The quantity (-1) cannot be negative.", error.Message);
        }
        #endregion RetrieveStockFromWarehouse

        #region ReserveStock
        [TestMethod]
        public async Task ReserveStock_echoes_CorrelationId()
        {
            var mock = new Mock<IWarehouse>(MockBehavior.Strict);
            mock.Setup(mag => mag.ReserveStockAsync(It.IsAny<ReserveStockCommand>()))
                .Returns(Task.FromResult(new ArticleStock()));

            var sut = new WarehouseController(mock.Object);

            var correlationId = Guid.NewGuid();
            var command = new ReserveStockCommand
            {
                CorrelationId = correlationId,
                ArticleNumber = 1106,
                Quantity = 6,
            };

            var result = await sut.ReserveStock(command);

            Assert.IsInstanceOfType(result, typeof(OkObjectResult));
            var okObjectResult = (OkObjectResult)result;
            Assert.IsInstanceOfType(okObjectResult.Value, typeof(WarehouseResult));
            WarehouseResult warehouseResult = (WarehouseResult)okObjectResult.Value!;
            Assert.AreEqual(correlationId, warehouseResult?.CorrelationId);
        }

        [TestMethod]
        public async Task ReserveStock_ReturnsNewStock()
        {
            var mock = new Mock<IWarehouse>(MockBehavior.Strict);
            mock.Setup(mag => mag.ReserveStockAsync(It.IsAny<ReserveStockCommand>()))
                .Returns(Task.FromResult(new ArticleStock
                {
                    ArticleNumber = 1301,
                    Stock = 11,
                    OfWhichReserved = 2,
                }));

            var sut = new WarehouseController(mock.Object);

            // Act
            var command = new ReserveStockCommand { Quantity = 1 };
            var result = await sut.ReserveStock(command);

            // Assert
            Assert.IsInstanceOfType(result, typeof(OkObjectResult));
            var okObjectResult = (OkObjectResult)result;
            Assert.IsInstanceOfType(okObjectResult.Value, typeof(WarehouseResult));
            WarehouseResult warehouseResult = (WarehouseResult)okObjectResult.Value!;

            Assert.AreEqual(1301, warehouseResult.ArticleNumber);
            Assert.AreEqual(11, warehouseResult.NewStock);
            Assert.AreEqual(2, warehouseResult.OfWhichReserved);
        }

        [TestMethod]
        public async Task ReserveStock_returns404_WhenArticleNotFound()
        {
            var mock = new Mock<IWarehouse>(MockBehavior.Strict);
            mock.Setup(mag => mag.ReserveStockAsync(It.IsAny<ReserveStockCommand>()))
                .ThrowsAsync(new ArticleNotFoundException(articleNumber: 1107));

            var sut = new WarehouseController(mock.Object);
            var command = new ReserveStockCommand
            {
                CorrelationId = Guid.NewGuid(),
                ArticleNumber = 1107,
                Quantity = 7,
            };
            var result = await sut.ReserveStock(command);

            Assert.IsInstanceOfType(result, typeof(NotFoundObjectResult));
            var notFoundObjectResult = (NotFoundObjectResult)result;
            Assert.IsInstanceOfType(notFoundObjectResult.Value, typeof(Error));
            var error = (Error)notFoundObjectResult.Value!;
            Assert.AreEqual("MAG001", error.Code);
            Assert.AreEqual("The article with ArticleNumber 1107 could not be found.", error.Message);
        }

        [TestMethod]
        public async Task ReserveStock_returns409_WhenReservedStockIsInsufficient()
        {
            var mock = new Mock<IWarehouse>(MockBehavior.Strict);
            mock.Setup(mag => mag.ReserveStockAsync(It.IsAny<ReserveStockCommand>()))
                .ThrowsAsync(new FunctionalException(new List<Error>() {
                    Errors.InsufficientReservedStock(1201, 6)
                }));

            var sut = new WarehouseController(mock.Object);
            var command = new ReserveStockCommand();
            var result = await sut.ReserveStock(command);

            Assert.IsInstanceOfType(result, typeof(ConflictObjectResult));
            var conflictObjectResult = (ConflictObjectResult)result;
            Assert.IsInstanceOfType(conflictObjectResult.Value, typeof(Error[]));
            var errors = (Error[])conflictObjectResult.Value!;
            Assert.AreEqual(1, errors.Length);
            Assert.AreEqual("MAG004", errors[0].Code);
            Assert.AreEqual(
                "For article with ArticleNumber 1201 the reserved stock of 6 is insufficient.",
                errors[0].Message);
        }

        [TestMethod]
        public async Task ReserveStock_WithNegativeQuantity_returnsError()
        {
            var mock = new Mock<IWarehouse>(MockBehavior.Strict);
            var sut = new WarehouseController(mock.Object);

            var command = new ReserveStockCommand
            {
                ArticleNumber = 1105,
                Quantity = -1,
            };

            var result = await sut.ReserveStock(command);

            Assert.IsInstanceOfType(result, typeof(UnprocessableEntityObjectResult));
            var unprocessableEntityObjectResult = (UnprocessableEntityObjectResult)result;
            Assert.IsInstanceOfType(unprocessableEntityObjectResult.Value, typeof(Error));
            var error = (Error)unprocessableEntityObjectResult.Value!;
            Assert.AreEqual("MAG003", error.Code);
            Assert.AreEqual("The quantity (-1) cannot be negative.", error.Message);
        }
        #endregion ReserveStock

        #region RetrieveReservedStockFromWarehouse
        [TestMethod]
        public async Task RetrieveReservedStockFromWarehouse_echoes_CorrelationId()
        {
            var mock = new Mock<IWarehouse>(MockBehavior.Strict);
            mock.Setup(mag => mag.RetrieveReservedStockAsync(It.IsAny<RetrieveReservedStockFromWarehouseCommand>()))
                .Returns(Task.FromResult(new ArticleStock()));

            var sut = new WarehouseController(mock.Object);

            var correlationId = Guid.NewGuid();
            var command = new RetrieveReservedStockFromWarehouseCommand
            {
                CorrelationId = correlationId,
                ArticleNumber = 1109,
                Quantity = 9,
            };

            var result = await sut.RetrieveReservedStockFromWarehouse(command);

            Assert.IsInstanceOfType(result, typeof(OkObjectResult));
            var okObjectResult = (OkObjectResult)result;
            Assert.IsInstanceOfType(okObjectResult.Value, typeof(WarehouseResult));
            var warehouseResult = (WarehouseResult)okObjectResult.Value!;
            Assert.AreEqual(correlationId, warehouseResult.CorrelationId);
        }

        [TestMethod]
        public async Task RetrieveReservedStockFromWarehouse_ReturnsNewStock()
        {
            var mock = new Mock<IWarehouse>(MockBehavior.Strict);
            mock.Setup(mag => mag.RetrieveReservedStockAsync(It.IsAny<RetrieveReservedStockFromWarehouseCommand>()))
                .Returns(Task.FromResult(new ArticleStock
                {
                    ArticleNumber = 1301,
                    Stock = 11,
                    OfWhichReserved = 5,
                }));
            var sut = new WarehouseController(mock.Object);

            var command = new RetrieveReservedStockFromWarehouseCommand();
            var result = await sut.RetrieveReservedStockFromWarehouse(command);

            Assert.IsInstanceOfType(result, typeof(OkObjectResult));
            var okObjectResult = (OkObjectResult)result;
            Assert.IsInstanceOfType(okObjectResult.Value, typeof(WarehouseResult));
            var warehouseResult = (WarehouseResult)okObjectResult.Value!;

            Assert.AreEqual(1301, warehouseResult.ArticleNumber);
            Assert.AreEqual(11, warehouseResult.NewStock);
            Assert.AreEqual(5, warehouseResult.OfWhichReserved);
        }

        [TestMethod]
        public async Task RetrieveReservedStockFromWarehouse_returns404_WhenArticleNotFound()
        {
            var mock = new Mock<IWarehouse>(MockBehavior.Strict);
            mock.Setup(mag => mag.RetrieveReservedStockAsync(It.IsAny<RetrieveReservedStockFromWarehouseCommand>()))
                .ThrowsAsync(new ArticleNotFoundException(articleNumber: 1110));

            var sut = new WarehouseController(mock.Object);
            var command = new RetrieveReservedStockFromWarehouseCommand();
            var result = await sut.RetrieveReservedStockFromWarehouse(command);

            Assert.IsInstanceOfType(result, typeof(NotFoundObjectResult));
            var notFoundObjectResult = (NotFoundObjectResult)result;
            Assert.IsInstanceOfType(notFoundObjectResult.Value, typeof(Error));
            var error = (Error)notFoundObjectResult.Value!;
            Assert.AreEqual("MAG001", error.Code);
            Assert.AreEqual("The article with ArticleNumber 1110 could not be found.", error.Message);
        }

        [TestMethod]
        public async Task RetrieveReservedStockFromWarehouse_WithNegativeQuantity_returnsError()
        {
            var mock = new Mock<IWarehouse>(MockBehavior.Strict);
            var sut = new WarehouseController(mock.Object);

            var command = new RetrieveReservedStockFromWarehouseCommand
            {
                ArticleNumber = 1105,
                Quantity = -1,
            };

            var result = await sut.RetrieveReservedStockFromWarehouse(command);

            Assert.IsInstanceOfType(result, typeof(UnprocessableEntityObjectResult));
            var unprocessableEntityObjectResult = (UnprocessableEntityObjectResult)result;
            Assert.IsInstanceOfType(unprocessableEntityObjectResult.Value, typeof(Error));
            var error = (Error)unprocessableEntityObjectResult.Value!;
            Assert.AreEqual("MAG003", error.Code);
            Assert.AreEqual("The quantity (-1) cannot be negative.", error.Message);
        }
        #endregion RetrieveReservedStockFromWarehouse

        #region ReleaseReservedStock
        [TestMethod]
        public async Task ReleaseReservedStock_echoes_CorrelationId()
        {
            var mock = new Mock<IWarehouse>(MockBehavior.Strict);
            mock.Setup(mag => mag.ReleaseReservedStockAsync(It.IsAny<ReleaseReservedStockCommand>()))
                .Returns(Task.FromResult(new ArticleStock()));

            var sut = new WarehouseController(mock.Object);

            var correlationId = Guid.NewGuid();
            var command = new ReleaseReservedStockCommand
            {
                CorrelationId = correlationId,
                ArticleNumber = 1109,
                Quantity = 9,
            };

            var result = await sut.ReleaseReservedStock(command);

            Assert.IsInstanceOfType(result, typeof(OkObjectResult));
            var okObjectResult = (OkObjectResult)result;
            Assert.IsInstanceOfType(okObjectResult.Value, typeof(WarehouseResult));
            var warehouseResult = (WarehouseResult)okObjectResult.Value!;
            Assert.AreEqual(correlationId, warehouseResult.CorrelationId);
        }

        [TestMethod]
        public async Task ReleaseReservedStock_ReturnsNewStock()
        {
            var mock = new Mock<IWarehouse>(MockBehavior.Strict);
            mock.Setup(mag => mag.ReleaseReservedStockAsync(It.IsAny<ReleaseReservedStockCommand>()))
                .Returns(Task.FromResult(new ArticleStock
                {
                    ArticleNumber = 1301,
                    Stock = 11,
                    OfWhichReserved = 5,
                }));
            var sut = new WarehouseController(mock.Object);

            var command = new ReleaseReservedStockCommand();
            var result = await sut.ReleaseReservedStock(command);

            Assert.IsInstanceOfType(result, typeof(OkObjectResult));
            var okObjectResult = (OkObjectResult)result;
            Assert.IsInstanceOfType(okObjectResult.Value, typeof(WarehouseResult));
            var warehouseResult = (WarehouseResult)okObjectResult.Value!;

            Assert.AreEqual(1301, warehouseResult.ArticleNumber);
            Assert.AreEqual(11, warehouseResult.NewStock);
            Assert.AreEqual(5, warehouseResult.OfWhichReserved);
        }

        [TestMethod]
        public async Task ReleaseReservedStock_returns404_WhenArticleNotFound()
        {
            var mock = new Mock<IWarehouse>(MockBehavior.Strict);
            mock.Setup(mag => mag.ReleaseReservedStockAsync(It.IsAny<ReleaseReservedStockCommand>()))
                .ThrowsAsync(new ArticleNotFoundException(articleNumber: 1110));

            var sut = new WarehouseController(mock.Object);
            var command = new ReleaseReservedStockCommand();
            var result = await sut.ReleaseReservedStock(command);

            Assert.IsInstanceOfType(result, typeof(NotFoundObjectResult));
            var notFoundObjectResult = (NotFoundObjectResult)result;
            Assert.IsInstanceOfType(notFoundObjectResult.Value, typeof(Error));
            var error = (Error)notFoundObjectResult.Value!;
            Assert.AreEqual("MAG001", error.Code);
            Assert.AreEqual("The article with ArticleNumber 1110 could not be found.", error.Message);
        }

        [TestMethod]
        public async Task ReleaseReservedStock_returns409_WhenReservedStockIsInsufficient()
        {
            var mock = new Mock<IWarehouse>(MockBehavior.Strict);
            mock.Setup(mag => mag.ReleaseReservedStockAsync(It.IsAny<ReleaseReservedStockCommand>()))
                .ThrowsAsync(new FunctionalException(new List<Error>() {
                    Errors.InsufficientReservedStock(1201, 3)
                }));

            var sut = new WarehouseController(mock.Object);
            var command = new ReleaseReservedStockCommand();
            var result = await sut.ReleaseReservedStock(command);

            Assert.IsInstanceOfType(result, typeof(ConflictObjectResult));
            var conflictObjectResult = (ConflictObjectResult)result;
            Assert.IsInstanceOfType(conflictObjectResult.Value, typeof(Error[]));
            var errors = (Error[])conflictObjectResult.Value!;
            Assert.AreEqual(1, errors.Length);
            Assert.AreEqual("MAG004", errors[0].Code);
            Assert.AreEqual(
                "For article with ArticleNumber 1201 the reserved stock of 3 is insufficient.",
                errors[0].Message);
        }

        [TestMethod]
        public async Task ReleaseReservedStock_WithNegativeQuantity_returnsError()
        {
            var mock = new Mock<IWarehouse>(MockBehavior.Strict);
            var sut = new WarehouseController(mock.Object);

            var command = new ReleaseReservedStockCommand
            {
                ArticleNumber = 1105,
                Quantity = -1,
            };

            var result = await sut.ReleaseReservedStock(command);

            Assert.IsInstanceOfType(result, typeof(UnprocessableEntityObjectResult));
            var unprocessableEntityObjectResult = (UnprocessableEntityObjectResult)result;
            Assert.IsInstanceOfType(unprocessableEntityObjectResult.Value, typeof(Error));
            var error = (Error)unprocessableEntityObjectResult.Value!;
            Assert.AreEqual("MAG003", error.Code);
            Assert.AreEqual("The quantity (-1) cannot be negative.", error.Message);
        }
        #endregion ReleaseReservedStock
    }
}
