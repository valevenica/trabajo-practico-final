using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace StockManufactura.Infrastructure.Db
{
    public sealed class StockManufacturaDesignTimeDbContextFactory : IDesignTimeDbContextFactory<StockManufacturaDbContext>
    {
        public StockManufacturaDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<StockManufacturaDbContext>();
            optionsBuilder.UseSqlite("Data Source=stockmanufactura.design.db");
            return new StockManufacturaDbContext(optionsBuilder.Options);
        }
    }
}
