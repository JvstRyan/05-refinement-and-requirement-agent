using Kantilever.WarehouseService.Commands;
using Kantilever.WarehouseService.Model;
using Thumper.Core;

namespace Kantilever.WarehouseService.Events
{
    public class StockPlacedInWarehouse : DomainEvent
    {
        public long ArticleNumber { get; set; }
        public int Quantity { get; set; }
        public int NewStock { get; set; }

        public StockPlacedInWarehouse(PlaceStockInWarehouseCommand command, ArticleStock articleStock)
        {
            CorrelationId = command.CorrelationId;
            ArticleNumber = command.ArticleNumber;
            Quantity = command.Quantity;
            NewStock = articleStock.Stock;
        }
    }
}