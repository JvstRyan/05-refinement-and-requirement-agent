using Kantilever.WarehouseService.DAL;
using Kantilever.WarehouseService.Logic;
using Microsoft.EntityFrameworkCore;
using Polly.Retry;
using Polly;
using Thumper.Core;

namespace Kantilever.WarehouseService
{
    public static class ProgramDIExtensions
    {
        public static RetryPolicy TryForMultipleSeconds =
            Policy.Handle<Exception>()
                  .WaitAndRetry(10, retry => TimeSpan.FromSeconds(1));

        public static IServiceCollection AddWarehouseDependencies(this IServiceCollection services, 
                string connectionString, IThumperOptions? options = null)
        {
            services.AddDbContext<WarehouseContext>(optionsBuilder =>
                optionsBuilder.UseNpgsql(connectionString)
            );
            if (options == null)
                services.AddThumper();
            else
               services.AddThumper(options);
            services.AddTransient<IWarehouse, Warehouse>();

            return services;
        }
    }
}
