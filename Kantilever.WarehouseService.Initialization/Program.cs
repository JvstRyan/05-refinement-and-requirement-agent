using Kantilever.WarehouseService.DAL;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Retry;

namespace Kantilever.WarehouseService.Initialization
{
    class Program
    {
        public static RetryPolicy TryForMultipleSeconds = 
            Policy.Handle<Exception>()
                  .WaitAndRetry(15, retry => TimeSpan.FromSeconds(3));

        static void Main(string[] args)
        {
            string? connectionString = Environment.GetEnvironmentVariable("WarehouseDB")
                ?? "Host=localhost;Port=5432;Database=WarehouseDB;Username=postgres;Password=Geheim_101";

            string status = String.IsNullOrWhiteSpace(connectionString) ? "empty" : "not empty";
            Console.WriteLine($"WarehouseService.Initialization: EnvironmentVariable 'WarehouseDB' has been read and is {status}");

            Console.WriteLine("WarehouseService.Initialization: establishing connection to PostgreSQL database ...");
            DbContextOptions<WarehouseContext> options = new DbContextOptionsBuilder<WarehouseContext>()
                .UseNpgsql(connectionString)
                .Options;

            Console.WriteLine("WarehouseService.Initialization: creating Database ...");
            using (var context = new WarehouseContext(options))
            {
                TryForMultipleSeconds.Execute(() =>
                {
                    context.Database.EnsureCreated();
                });

                Console.WriteLine("WarehouseService.Initialization: populating database ...");
                if (!context.ArticleStocks.Any())
                {
                    context.AddRange(WarehouseSnapshotData.Stock);
                    context.SaveChanges();
                }
            }

            Console.WriteLine("WarehouseService.Initialization COMPLETED!");
        }
    }
}
