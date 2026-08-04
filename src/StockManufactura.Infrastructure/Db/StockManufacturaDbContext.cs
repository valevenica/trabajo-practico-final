using Microsoft.EntityFrameworkCore;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Infrastructure.Db
{
    public class StockManufacturaDbContext : DbContext
    {
        public StockManufacturaDbContext(DbContextOptions<StockManufacturaDbContext> options) : base(options)
        {
        }

        public DbSet<Usuario> Usuarios { get; set; } = null!;
        public DbSet<Rol> Roles { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(StockManufacturaDbContext).Assembly);
            base.OnModelCreating(modelBuilder);
        }
    }
}
