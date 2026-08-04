using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StockManufactura.Application.Interfaces;
using StockManufactura.Domain.Entities;
using StockManufactura.Infrastructure.Db;

namespace StockManufactura.Infrastructure.Repositories
{
    public sealed class ProveedorRepository : Repository<Proveedor>, IProveedorRepository
    {
        private readonly StockManufacturaDbContext _context;

        public ProveedorRepository(StockManufacturaDbContext context) : base(context)
        {
            _context = context;
        }

        public Task<Proveedor?> GetByCuitAsync(string cuit)
        {
            return _context.Proveedores.FirstOrDefaultAsync(x => x.Cuit == cuit);
        }
    }
}
