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
    public sealed class ResourcePriceHistoryRepository : Repository<ResourcePriceHistory>, IResourcePriceHistoryRepository
    {
        private readonly StockManufacturaDbContext _context;

        public ResourcePriceHistoryRepository(StockManufacturaDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<ResourcePriceHistory>> ListByResourceAsync(Guid recursoId)
        {
            return await _context.ResourcePriceHistory
                .AsNoTracking()
                .Where(x => x.RecursoId == recursoId)
                .OrderByDescending(x => x.Fecha)
                .ToListAsync();
        }
    }
}
