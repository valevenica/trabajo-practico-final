using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StockManufactura.Application.Interfaces;
using StockManufactura.Domain.Entities;
using StockManufactura.Infrastructure.Db;

namespace StockManufactura.Infrastructure.Repositories
{
    public sealed class ProductoRepository : Repository<Producto>, IProductoRepository
    {
        private readonly StockManufacturaDbContext _context;

        public ProductoRepository(StockManufacturaDbContext context) : base(context)
        {
            _context = context;
        }

        public Task<Producto?> GetByCodigoAsync(string codigo)
        {
            return _context.Productos.FirstOrDefaultAsync(x => x.Codigo == codigo);
        }

        public async Task<IReadOnlyList<Producto>> ListActivosAsync()
        {
            return await _context.Productos.AsNoTracking().Where(x => x.Activo).OrderBy(x => x.Nombre).ToListAsync();
        }

        public async Task<IReadOnlyList<Producto>> ListByIdsAsync(IEnumerable<Guid> ids)
        {
            var list = ids.ToList();
            if (list.Count == 0)
            {
                return Array.Empty<Producto>();
            }

            return await _context.Productos.AsNoTracking().Where(x => list.Contains(x.Id)).ToListAsync();
        }
    }
}
