using StockManufactura.Application.Interfaces;
using StockManufactura.Domain.Entities;
using StockManufactura.Infrastructure.Db;

namespace StockManufactura.Infrastructure.Repositories
{
    public sealed class UsuarioRepository : Repository<Usuario>, IUsuarioRepository
    {
        public UsuarioRepository(StockManufacturaDbContext context) : base(context)
        {
        }
    }
}
