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
    public sealed class ProductCostSnapshotItemRepository : Repository<ProductCostSnapshotItem>, IProductCostSnapshotItemRepository
    {
        private readonly StockManufacturaDbContext _context;

        public ProductCostSnapshotItemRepository(StockManufacturaDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<ProductCostSnapshotItem>> ListBySnapshotAsync(Guid snapshotId)
        {
            return await _context.ProductCostSnapshotItems
                .AsNoTracking()
                .Include(x => x.Recurso)
                .Where(x => x.SnapshotId == snapshotId)
                .OrderBy(x => x.Recurso.Nombre)
                .ToListAsync();
        }
    }
}
