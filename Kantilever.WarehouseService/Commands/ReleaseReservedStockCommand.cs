namespace Kantilever.WarehouseService
{
    public class ReleaseReservedStockCommand
    {
        public Guid CorrelationId { get; set; }
        public long ArticleNumber { get; set; }
        public int Quantity { get; set; }
    }
}