namespace Kantilever.WarehouseService.Model
{
    public class ArticleStock
    {
        public long Id { get; set; }   
        public long ArticleNumber { get; set; }
        public int Stock { get; set; }
        public int OfWhichReserved { get; set; }

        public ArticleStock(long articleNumber = 0, int stock = 0, int ofWhichReserved = 0)
        {
            ArticleNumber = articleNumber;
            Stock = stock;
            OfWhichReserved = ofWhichReserved;
        }
    }
}