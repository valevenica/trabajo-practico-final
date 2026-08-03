using Microsoft.EntityFrameworkCore;

namespace StockManufactura.Infrastructure.Db
{
    public class StockManufacturaDbContext : DbContext
    {
        public StockManufacturaDbContext(DbContextOptions<StockManufacturaDbContext> options) : base(options)
        {
        }

        // DbSets go here, e.g.:
        // public DbSet<Product> Products { get; set; }
    }
}
