using Kantilever.WarehouseService.Model;
using Microsoft.EntityFrameworkCore;

namespace Kantilever.WarehouseService.DAL
{
    public class WarehouseContext : DbContext
    {
        public WarehouseContext(DbContextOptions<WarehouseContext> options) : base(options)
        {
        }

        public DbSet<ArticleStock> ArticleStocks { get; set; }
    }
}