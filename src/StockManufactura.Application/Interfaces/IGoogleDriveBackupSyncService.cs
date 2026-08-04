using System.Threading;
using System.Threading.Tasks;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Application.Interfaces
{
    public interface IGoogleDriveBackupSyncService
    {
        Task<bool> TryUploadAsync(BackupRecord backupRecord, BackupSettings settings, CancellationToken cancellationToken = default);
    }
}
