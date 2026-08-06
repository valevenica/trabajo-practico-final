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
    public sealed class RecursoProveedorRepository : Repository<RecursoProveedor>, IRecursoProveedorRepository
    {
        private readonly StockManufacturaDbContext _context;

        public RecursoProveedorRepository(StockManufacturaDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<RecursoProveedor>> ListByRecursoIdAsync(Guid recursoId)
        {
            var items = await _context.RecursoProveedores
                .AsNoTracking()
                .Include(x => x.Proveedor)
                .Where(x => x.RecursoId == recursoId)
                .ToListAsync();

            return items
                .OrderByDescending(x => x.EsPrioritario)
                .ThenBy(x => x.Proveedor?.Nombre ?? string.Empty)
                .ToList();
        }

        public Task<RecursoProveedor?> GetPrioritarioAsync(Guid recursoId)
        {
            return _context.RecursoProveedores
                .AsNoTracking()
                .Include(x => x.Proveedor)
                .FirstOrDefaultAsync(x => x.RecursoId == recursoId && x.EsPrioritario);
        }
    }
}
