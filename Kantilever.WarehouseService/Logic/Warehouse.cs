using Kantilever.WarehouseService.Commands;
using Kantilever.WarehouseService.DAL;
using Kantilever.WarehouseService.Events;
using Kantilever.WarehouseService.Exceptions;
using Kantilever.WarehouseService.Model;
using Microsoft.EntityFrameworkCore;
using Thumper.Core;

namespace Kantilever.WarehouseService.Logic
{
    public class Warehouse : IWarehouse
    {
        private readonly WarehouseContext _context;
        private readonly IThumperPublisher _publisher;

        public Warehouse(WarehouseContext context, IThumperPublisher publisher)
        {
            _context = context;
            _publisher = publisher;
        }

        public async Task<ArticleStock> FindArticleStockAsync(long articleNumber)
        {
            var articleStock = await _context.ArticleStocks
                .FirstOrDefaultAsync(av => av.ArticleNumber == articleNumber);

            if (articleStock == null)
            {
                throw new ArticleNotFoundException(articleNumber);
            }

            return articleStock;
        }

        public async Task<ArticleStock> PlaceStockInAsync(PlaceStockInWarehouseCommand command)
        {
            var articleStock = await _context.ArticleStocks
                .FirstOrDefaultAsync(av => av.ArticleNumber == command.ArticleNumber);

            if (articleStock == null)
            {
                articleStock = new ArticleStock(command.ArticleNumber, stock: 0, ofWhichReserved: 0);
                _context.ArticleStocks.Add(articleStock);
            }

            articleStock.Stock += command.Quantity;
            await _context.SaveChangesAsync();

            var evt = new StockPlacedInWarehouse(command, articleStock);
            await _publisher.PublishAsync("Kantilever.WarehouseService.StockPlacedInWarehouse", evt);

            return articleStock;
        }

        public async Task<ArticleStock> RetrieveStockAsync(RetrieveStockFromWarehouseCommand command)
        {
            ArticleStock articleStock = await FindArticleStockAsync(command.ArticleNumber);

            int availableStock = articleStock.Stock - articleStock.OfWhichReserved;
            if (availableStock < command.Quantity)
            {
                var errors = new List<Error> { 
                    Errors.InsufficientStock(command.ArticleNumber, availableStock)
                };
                throw new FunctionalException(errors);
            }

            articleStock.Stock -= command.Quantity;
            await _context.SaveChangesAsync();

            var evt = new StockRetrievedFromWarehouse(command, articleStock);
            await _publisher.PublishAsync("Kantilever.WarehouseService.StockRetrievedFromWarehouse", evt);

            return articleStock;
        }

        public async Task<ArticleStock> ReserveStockAsync(ReserveStockCommand command)
        {
            ArticleStock articleStock = await FindArticleStockAsync(command.ArticleNumber);

            int availableStock = articleStock.Stock - articleStock.OfWhichReserved;
            if (availableStock < command.Quantity)
            {
                var errors = new List<Error> {
                    Errors.InsufficientStock(command.ArticleNumber, availableStock)
                };
                throw new FunctionalException(errors);
            }

            articleStock.OfWhichReserved += command.Quantity;
            await _context.SaveChangesAsync();

            var evt = new StockReserved(command, articleStock);
            await _publisher.PublishAsync("Kantilever.WarehouseService.StockReserved", evt);

            return articleStock;
        }

        public async Task<ArticleStock> ReserveStockEvenIfUnavailableAsync(ReserveStockEvenIfUnavailableCommand command)
        {
            ArticleStock articleStock = await FindArticleStockAsync(command.ArticleNumber);

            articleStock.OfWhichReserved += command.Quantity;
            await _context.SaveChangesAsync();

            var evt = new StockReserved(command, articleStock);
            await _publisher.PublishAsync("Kantilever.WarehouseService.StockReserved", evt);

            return articleStock;
        }

        public async Task<ArticleStock> RetrieveReservedStockAsync(RetrieveReservedStockFromWarehouseCommand command)
        {
            ArticleStock articleStock = await FindArticleStockAsync(command.ArticleNumber);

            if (articleStock.OfWhichReserved < command.Quantity)
            {
                var errors = new List<Error> {
                    Errors.InsufficientReservedStock(command.ArticleNumber, 
                        articleStock.OfWhichReserved)
                };
                throw new FunctionalException(errors);
            }

            articleStock.Stock -= command.Quantity;
            articleStock.OfWhichReserved -= command.Quantity;
            await _context.SaveChangesAsync();

            var evt = new ReservedStockRetrievedFromWarehouse(command, articleStock);
            await _publisher.PublishAsync("Kantilever.WarehouseService.ReservedStockRetrievedFromWarehouse", evt);

            return articleStock;
        }

        public async Task<ArticleStock> ReleaseReservedStockAsync(ReleaseReservedStockCommand command)
        {
            ArticleStock articleStock = await FindArticleStockAsync(command.ArticleNumber);

            if (articleStock.OfWhichReserved < command.Quantity)
            {
                var errors = new List<Error> {
                    Errors.InsufficientReservedStock(command.ArticleNumber,
                        articleStock.OfWhichReserved)
                };
                throw new FunctionalException(errors);
            }

            articleStock.OfWhichReserved -= command.Quantity;
            await _context.SaveChangesAsync();

            var evt = new ReservedStockReleased(command, articleStock);
            await _publisher.PublishAsync("Kantilever.WarehouseService.ReservedStockReleased", evt);

            return articleStock;
        }
    }
}
