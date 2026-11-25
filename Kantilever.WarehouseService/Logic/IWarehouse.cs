using Kantilever.WarehouseService.Commands;
using Kantilever.WarehouseService.Model;

namespace Kantilever.WarehouseService.Logic
{
    public interface IWarehouse
    {
        Task<ArticleStock> FindArticleStockAsync(long articleNumber);
        Task<ArticleStock> PlaceStockInAsync(PlaceStockInWarehouseCommand command);
        Task<ArticleStock> RetrieveStockAsync(RetrieveStockFromWarehouseCommand command);
        Task<ArticleStock> ReserveStockAsync(ReserveStockCommand command);
        Task<ArticleStock> ReserveStockEvenIfUnavailableAsync(ReserveStockEvenIfUnavailableCommand command);
        Task<ArticleStock> RetrieveReservedStockAsync(RetrieveReservedStockFromWarehouseCommand command);
        Task<ArticleStock> ReleaseReservedStockAsync(ReleaseReservedStockCommand releaseReservedStockCommand);
    }
}