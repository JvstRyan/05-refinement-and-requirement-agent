using Kantilever.WarehouseService.DAL;
using Kantilever.WarehouseService.Model;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Kantilever.WarehouseService.Repositories
{
    public class WarehouseRepository
    {
        private readonly WarehouseContext _context;

        public WarehouseRepository(WarehouseContext context)
        {
            _context = context;
        }

        public async Task<ArticleStock> GetArticleStockByArticleNumberAsync(long articleNumber)
        {
            return await _context.ArticleStocks
                .FirstOrDefaultAsync(av => av.ArticleNumber == articleNumber);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
