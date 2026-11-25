namespace Kantilever.WarehouseService.Controllers
{
    public class WarehouseResult
    {
        public Guid CorrelationId { get; set; }
        public long ArticleNumber { get; set; }
        public int NewStock { get; set; }
        public int OfWhichReserved { get; set; }
    }
}