using StockManufactura.Application.Interfaces;
using StockManufactura.Domain.Entities;
using StockManufactura.Infrastructure.Db;

namespace StockManufactura.Infrastructure.Repositories
{
    public sealed class RolRepository : Repository<Rol>, IRolRepository
    {
        public RolRepository(StockManufacturaDbContext context) : base(context)
        {
        }
    }
}
