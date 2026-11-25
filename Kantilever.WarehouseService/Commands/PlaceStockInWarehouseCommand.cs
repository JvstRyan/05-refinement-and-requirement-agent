namespace Kantilever.WarehouseService.Commands
{
    public class PlaceStockInWarehouseCommand
    {
        public Guid CorrelationId { get; set; }
        public long ArticleNumber { get; set; }
        public int Quantity { get; set; }
    }
}