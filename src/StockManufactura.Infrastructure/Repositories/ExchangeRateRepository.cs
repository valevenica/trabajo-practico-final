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
            return _context.ExchangeRates
                .AsNoTracking()
                .OrderByDescending(x => x.Fecha)
                .FirstOrDefaultAsync();
        }
    }
}
