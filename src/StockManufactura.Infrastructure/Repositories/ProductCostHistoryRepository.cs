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
    public sealed class ProductCostHistoryRepository : Repository<ProductCostHistory>, IProductCostHistoryRepository
    {
        private readonly StockManufacturaDbContext _context;

        public ProductCostHistoryRepository(StockManufacturaDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<ProductCostHistory>> ListByProductAsync(Guid productId)
        {
            return await _context.ProductCostHistory
                .AsNoTracking()
                .Include(x => x.Product)
                .Where(x => x.ProductId == productId)
                .OrderByDescending(x => x.Fecha)
                .ToListAsync();
        }
    }
}
