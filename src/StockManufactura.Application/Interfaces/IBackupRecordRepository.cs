using System.Collections.Generic;
using System.Threading.Tasks;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Application.Interfaces
{
    public interface IBackupRecordRepository : IRepository<BackupRecord>
    {
        Task<IReadOnlyList<BackupRecord>> ListRecentAsync(int top);
    }
}
