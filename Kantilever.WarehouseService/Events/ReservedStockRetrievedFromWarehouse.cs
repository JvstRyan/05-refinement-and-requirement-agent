using Kantilever.WarehouseService.Model;
using Thumper.Core;

namespace Kantilever.WarehouseService
{
    public class ReservedStockRetrievedFromWarehouse : DomainEvent
    {
        public long ArticleNumber { get; set; }
        public int Quantity { get; set; }
        public int NewStock { get; set; }
        public int NewReservedQuantity { get; set; }

        public ReservedStockRetrievedFromWarehouse(RetrieveReservedStockFromWarehouseCommand command, ArticleStock articleStock)
        {
            CorrelationId = command.CorrelationId;
            ArticleNumber = command.ArticleNumber;
            Quantity = command.Quantity;
            NewStock = articleStock.Stock;
            NewReservedQuantity = articleStock.OfWhichReserved;
        }
    }
}