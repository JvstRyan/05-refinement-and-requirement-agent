using Kantilever.WarehouseService.Model;
using Thumper.Core;

namespace Kantilever.WarehouseService
{
    public class StockRetrievedFromWarehouse : DomainEvent
    {
        public long ArticleNumber { get; set; }
        public int Quantity { get; set; }
        public int NewStock { get; set; }

        public StockRetrievedFromWarehouse(RetrieveStockFromWarehouseCommand command, ArticleStock articleStock)
        {
            CorrelationId = command.CorrelationId;
            ArticleNumber = command.ArticleNumber;
            Quantity = command.Quantity;
            NewStock = articleStock.Stock;
        }
    }
}