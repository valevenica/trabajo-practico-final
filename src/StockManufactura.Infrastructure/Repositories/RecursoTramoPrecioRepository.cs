using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StockManufactura.Application.Interfaces;
using StockManufactura.Domain.Entities;
using StockManufactura.Infrastructure.Db;

namespace StockManufactura.Infrastructure.Repositories
{
    public sealed class RecursoTramoPrecioRepository : Repository<RecursoTramoPrecio>, IRecursoTramoPrecioRepository
    {
        private readonly StockManufacturaDbContext _context;

        public RecursoTramoPrecioRepository(StockManufacturaDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<RecursoTramoPrecio>> ListByRecursoIdAsync(Guid recursoId)
        {
            return await _context.RecursoTramosPrecio
                .AsNoTracking()
                .Where(x => x.RecursoId == recursoId)
                .OrderBy(x => x.Orden)
                .ToListAsync();
        }

        public async Task DeleteByRecursoIdAsync(Guid recursoId)
        {
            var items = await _context.RecursoTramosPrecio
                .Where(x => x.RecursoId == recursoId)
                .ToListAsync();

            _context.RecursoTramosPrecio.RemoveRange(items);
        }
    }
}
