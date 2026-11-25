using Kantilever.WarehouseService.Model;
using Thumper.Core;

namespace Kantilever.WarehouseService
{
    public class ReservedStockReleased : DomainEvent
    {
        public long ArticleNumber { get; set; }
        public int Quantity { get; set; }
        public int NewReservedQuantity { get; set; }

        public ReservedStockReleased(ReleaseReservedStockCommand command, ArticleStock articleStock)
        {
            CorrelationId = command.CorrelationId;
            ArticleNumber = command.ArticleNumber;
            Quantity = command.Quantity;
            NewReservedQuantity = articleStock.OfWhichReserved;
        }
    }
}