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
    public sealed class StockRepository : Repository<Stock>, IStockRepository
    {
        private readonly StockManufacturaDbContext _context;

        public StockRepository(StockManufacturaDbContext context) : base(context)
        {
            _context = context;
        }

        public Task<Stock?> GetByProductoYUbicacionAsync(Guid productoId, Guid ubicacionId)
        {
            return _context.Stocks.FirstOrDefaultAsync(x => x.ProductoId == productoId && x.UbicacionId == ubicacionId);
        }

        public async Task<IReadOnlyList<Stock>> ListByProductoAsync(Guid productoId)
        {
            return await _context.Stocks
                .AsNoTracking()
                .Where(x => x.ProductoId == productoId)
                .OrderBy(x => x.UbicacionId)
                .ToListAsync();
        }
    }
}
