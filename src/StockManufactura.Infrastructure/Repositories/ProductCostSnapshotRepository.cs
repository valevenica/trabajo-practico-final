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
    public sealed class ProductCostSnapshotRepository : Repository<ProductCostSnapshot>, IProductCostSnapshotRepository
    {
        private readonly StockManufacturaDbContext _context;

        public ProductCostSnapshotRepository(StockManufacturaDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<ProductCostSnapshot>> ListByProductAsync(Guid productId)
        {
            return await _context.ProductCostSnapshots
                .AsNoTracking()
                .Include(x => x.Product)
                .Where(x => x.ProductId == productId)
                .OrderByDescending(x => x.Fecha)
                .ToListAsync();
        }
    }
}
