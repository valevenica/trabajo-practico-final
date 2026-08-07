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
    public sealed class RecetaProductoItemRepository : Repository<RecetaProductoItem>, IRecetaProductoItemRepository
    {
        private readonly StockManufacturaDbContext _context;

        public RecetaProductoItemRepository(StockManufacturaDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<RecetaProductoItem>> ListByProductIdAsync(Guid productId)
        {
            return await _context.RecetaProductoItems
                .Include(x => x.Recurso)
                .Include(x => x.ComponenteProducto)
                .Where(x => x.ProductoId == productId)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<RecetaProductoItem>> ListByProductIdsAsync(IEnumerable<Guid> productIds)
        {
            var ids = productIds.ToList();
            if (ids.Count == 0)
            {
                return Array.Empty<RecetaProductoItem>();
            }

            return await _context.RecetaProductoItems
                .Include(x => x.Recurso)
                .Include(x => x.ComponenteProducto)
                .Where(x => ids.Contains(x.ProductoId))
                .ToListAsync();
        }

        public async Task<IReadOnlyList<RecetaProductoItem>> ListByResourceIdAsync(Guid resourceId)
        {
            return await _context.RecetaProductoItems
                .Include(x => x.Producto)
                .Include(x => x.Recurso)
                .Where(x => x.RecursoId == resourceId)
                .ToListAsync();
        }
    }
}
