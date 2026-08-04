using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StockManufactura.Application.Interfaces;
using StockManufactura.Domain.Entities;
using StockManufactura.Infrastructure.Db;

namespace StockManufactura.Infrastructure.Repositories
{
    public sealed class RecursoRepository : Repository<Recurso>, IRecursoRepository
    {
        private readonly StockManufacturaDbContext _context;

        public RecursoRepository(StockManufacturaDbContext context) : base(context)
        {
            _context = context;
        }

        public Task<Recurso?> GetByCodigoAsync(string codigo)
        {
            return _context.Recursos
                .Include(x => x.ProveedorHabitual)
                .FirstOrDefaultAsync(x => x.Codigo == codigo);
        }

        public async Task<IReadOnlyList<Recurso>> ListActivosAsync()
        {
            return await _context.Recursos
                .AsNoTracking()
                .Include(x => x.ProveedorHabitual)
                .Where(x => x.Activo)
                .OrderBy(x => x.Nombre)
                .ToListAsync();
        }
    }
}
