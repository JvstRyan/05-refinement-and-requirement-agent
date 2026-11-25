using Kantilever.WarehouseService.Commands;
using Kantilever.WarehouseService.Exceptions;
using Kantilever.WarehouseService.Logic;
using Kantilever.WarehouseService.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace Kantilever.WarehouseService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WarehouseController : ControllerBase
    {
        private readonly IWarehouse _warehouse;

        public WarehouseController(IWarehouse warehouse)
        {
            _warehouse = warehouse;
        }

        [HttpGet]
        [ProducesResponseType(typeof(WarehouseResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Stock(long articleNumber)
        {
            try
            {
                ArticleStock av = await _warehouse.FindArticleStockAsync(articleNumber);
                var result = new WarehouseResult
                {
                    CorrelationId = Guid.NewGuid(),
                    ArticleNumber = av.ArticleNumber,
                    NewStock = av.Stock,
                    OfWhichReserved = av.OfWhichReserved,
                };
                return Ok(result);
            }
            catch (ArticleNotFoundException ex)
            {
                return NotFound(ex.Error);
            }
        }

        [HttpPost("PlaceStockIn")]
        [ProducesResponseType(typeof(WarehouseResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> PlaceStockInWarehouse([FromBody] PlaceStockInWarehouseCommand command)
        {
            if (command.Quantity < 0)
            {
                var error = Errors.QuantityCannotBeNegative(command.Quantity);
                return UnprocessableEntity(error);
            }

            ArticleStock av = await _warehouse.PlaceStockInAsync(command);
            var result = new WarehouseResult
            {
                CorrelationId = command.CorrelationId,
                ArticleNumber = av.ArticleNumber,
                NewStock = av.Stock,
                OfWhichReserved = av.OfWhichReserved,
            };
            return Ok(result);
        }

        [HttpPost("RetrieveStock")]
        [ProducesResponseType(typeof(WarehouseResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(typeof(Error[]), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> RetrieveStockFromWarehouse(RetrieveStockFromWarehouseCommand command)
        {
            if (command.Quantity < 0)
            {
                var error = Errors.QuantityCannotBeNegative(command.Quantity);
                return UnprocessableEntity(error);
            }
            try
            {
                ArticleStock av = await _warehouse.RetrieveStockAsync(command);
                var result = new WarehouseResult
                {
                    CorrelationId = command.CorrelationId,
                    ArticleNumber = av.ArticleNumber,
                    NewStock = av.Stock,
                    OfWhichReserved = av.OfWhichReserved,
                };
                return Ok(result);
            }
            catch (ArticleNotFoundException ex)
            {
                return NotFound(ex.Error);
            }
            catch (FunctionalException ex)
            {
                return Conflict(ex.Errors.ToArray());
            }
        }

        [HttpPost("ReserveStock")]
        [ProducesResponseType(typeof(WarehouseResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(Error[]), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> ReserveStock(ReserveStockCommand command)
        {
            if (command.Quantity < 0)
            {
                var error = Errors.QuantityCannotBeNegative(command.Quantity);
                return UnprocessableEntity(error);
            }
            try
            {
                ArticleStock av = await _warehouse.ReserveStockAsync(command);
                var result = new WarehouseResult
                {
                    CorrelationId = command.CorrelationId,
                    ArticleNumber = av.ArticleNumber,
                    NewStock = av.Stock,
                    OfWhichReserved = av.OfWhichReserved,
                };
                return Ok(result);
            }
            catch (ArticleNotFoundException ex)
            {
                return NotFound(ex.Error);
            }
            catch (FunctionalException ex)
            {
                return Conflict(ex.Errors.ToArray());
            }
        }

        [HttpPost("ReserveStockEvenIfUnavailable")]
        [ProducesResponseType(typeof(WarehouseResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(Error[]), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> ReserveStockEvenIfUnavailable(ReserveStockEvenIfUnavailableCommand command)
        {
            if (command.Quantity < 0)
            {
                var error = Errors.QuantityCannotBeNegative(command.Quantity);
                return UnprocessableEntity(error);
            }
            try
            {
                ArticleStock av = await _warehouse.ReserveStockEvenIfUnavailableAsync(command);
                var result = new WarehouseResult
                {
                    CorrelationId = command.CorrelationId,
                    ArticleNumber = av.ArticleNumber,
                    NewStock = av.Stock,
                    OfWhichReserved = av.OfWhichReserved,
                };
                return Ok(result);
            }
            catch (ArticleNotFoundException ex)
            {
                return NotFound(ex.Error);
            }
            catch (FunctionalException ex)
            {
                return Conflict(ex.Errors.ToArray());
            }
        }

        [HttpPost("RetrieveReservedStock")]
        [ProducesResponseType(typeof(WarehouseResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(Error[]), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> RetrieveReservedStockFromWarehouse(RetrieveReservedStockFromWarehouseCommand command)
        {
            if (command.Quantity < 0)
            {
                var error = Errors.QuantityCannotBeNegative(command.Quantity);
                return UnprocessableEntity(error);
            }
            try
            {
                ArticleStock av = await _warehouse.RetrieveReservedStockAsync(command);
                var result = new WarehouseResult
                {
                    CorrelationId = command.CorrelationId,
                    ArticleNumber = av.ArticleNumber,
                    NewStock = av.Stock,
                    OfWhichReserved = av.OfWhichReserved,
                };
                return Ok(result);
            }
            catch (ArticleNotFoundException ex)
            {
                return NotFound(ex.Error);
            }
            catch (FunctionalException ex)
            {
                return Conflict(ex.Errors.ToArray());
            }
        }

        [HttpPost("ReleaseReservedStock")]
        [ProducesResponseType(typeof(WarehouseResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(Error[]), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> ReleaseReservedStock(ReleaseReservedStockCommand command)
        {
            if (command.Quantity < 0)
            {
                var error = Errors.QuantityCannotBeNegative(command.Quantity);
                return UnprocessableEntity(error);
            }
            try
            {
                ArticleStock av = await _warehouse.ReleaseReservedStockAsync(command);
                var result = new WarehouseResult
                {
                    CorrelationId = command.CorrelationId,
                    ArticleNumber = av.ArticleNumber,
                    NewStock = av.Stock,
                    OfWhichReserved = av.OfWhichReserved,
                };
                return Ok(result);
            }
            catch (ArticleNotFoundException ex)
            {
                return NotFound(ex.Error);
            }
            catch (FunctionalException ex)
            {
                return Conflict(ex.Errors.ToArray());
            }
        }
    }
}
