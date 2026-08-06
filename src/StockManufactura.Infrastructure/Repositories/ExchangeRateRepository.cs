using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StockManufactura.Application.Interfaces;
using StockManufactura.Domain.Entities;
using StockManufactura.Infrastructure.Db;

namespace StockManufactura.Infrastructure.Repositories
{
    public sealed class ExchangeRateRepository : Repository<ExchangeRate>, IExchangeRateRepository
    {
        private readonly StockManufacturaDbContext _context;

        public ExchangeRateRepository(StockManufacturaDbContext context) : base(context)
        {
            _context = context;
        }

        public Task<ExchangeRate?> GetLatestAsync()
        {
            // Prioritaria first, otherwise fall back to most recent
            return _context.ExchangeRates
                .AsNoTracking()
                .OrderByDescending(x => x.EsPrioritaria)
                .ThenByDescending(x => x.Fecha)
                .FirstOrDefaultAsync();
        }

        public async Task SetPrioritariaAsync(Guid id)
        {
            var all = await _context.ExchangeRates.ToListAsync();
            foreach (var rate in all)
                rate.EsPrioritaria = rate.Id == id;
        }
    }
}
