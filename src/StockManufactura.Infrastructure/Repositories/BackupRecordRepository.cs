using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StockManufactura.Application.Interfaces;
using StockManufactura.Domain.Entities;
using StockManufactura.Infrastructure.Db;

namespace StockManufactura.Infrastructure.Repositories
{
    public sealed class BackupRecordRepository : Repository<BackupRecord>, IBackupRecordRepository
    {
        private readonly StockManufacturaDbContext _context;

        public BackupRecordRepository(StockManufacturaDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<BackupRecord>> ListRecentAsync(int top)
        {
            return await _context.BackupRecords.AsNoTracking().OrderByDescending(x => x.FechaHora).Take(top).ToListAsync();
        }
    }
}
