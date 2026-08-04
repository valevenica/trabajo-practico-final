using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StockManufactura.Application.Interfaces;
using StockManufactura.Domain.Entities;
using StockManufactura.Infrastructure.Db;

namespace StockManufactura.Infrastructure.Repositories
{
    public sealed class OrdenProduccionRepository : Repository<OrdenProduccion>, IOrdenProduccionRepository
    {
        private readonly StockManufacturaDbContext _context;

        public OrdenProduccionRepository(StockManufacturaDbContext context) : base(context)
        {
            _context = context;
        }

        public Task<OrdenProduccion?> GetByCodigoAsync(string codigo)
        {
            return _context.OrdenesProduccion.FirstOrDefaultAsync(x => x.Codigo == codigo);
        }

        public async Task<IReadOnlyList<OrdenProduccion>> ListByCreatedDescAsync()
        {
            return await _context.OrdenesProduccion
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }
    }
}
