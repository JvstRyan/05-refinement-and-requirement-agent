using Thumper.Core;

namespace Kantilever.WarehouseService.IntegrationTest
{
    internal class EventData : DomainEvent
    {
        public long ArticleNumber { get; set; }
        public int Quantity { get; set; }
        public int NewStock { get; set; }
        public int NewReservedQuantity { get; set; }
    }
}