using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StockManufactura.Application.Interfaces;
using StockManufactura.Domain.Entities;
using StockManufactura.Infrastructure.Db;

namespace StockManufactura.Infrastructure.Repositories
{
    public sealed class BackupSettingsRepository : Repository<BackupSettings>, IBackupSettingsRepository
    {
        private readonly StockManufacturaDbContext _context;

        public BackupSettingsRepository(StockManufacturaDbContext context) : base(context)
        {
            _context = context;
        }

        public Task<BackupSettings?> GetCurrentAsync()
        {
            return _context.BackupSettings.FirstOrDefaultAsync();
        }
    }
}
