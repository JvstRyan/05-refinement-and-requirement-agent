using Kantilever.WarehouseService.Model;
using Thumper.Core;

namespace Kantilever.WarehouseService
{
    public class StockReserved : DomainEvent
    {
        public long ArticleNumber { get; set; }
        public int Quantity { get; set; }
        public int NewReservedQuantity { get; set; }

        public StockReserved(ReserveStockCommand command, ArticleStock articleStock)
        {
            CorrelationId = command.CorrelationId;
            ArticleNumber = command.ArticleNumber;
            Quantity = command.Quantity;
            NewReservedQuantity = articleStock.OfWhichReserved;
        }

        public StockReserved(ReserveStockEvenIfUnavailableCommand command, ArticleStock articleStock)
        {
            CorrelationId = command.CorrelationId;
            ArticleNumber = command.ArticleNumber;
            Quantity = command.Quantity;
            NewReservedQuantity = articleStock.OfWhichReserved;
        }
    }
}