using System.Threading.Tasks;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Application.Interfaces
{
    public interface IBackupSettingsRepository : IRepository<BackupSettings>
    {
        Task<BackupSettings?> GetCurrentAsync();
    }
}
