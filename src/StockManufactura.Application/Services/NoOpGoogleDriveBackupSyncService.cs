using System.Threading;
using System.Threading.Tasks;
using StockManufactura.Application.Interfaces;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Application.Services
{
    public sealed class NoOpGoogleDriveBackupSyncService : IGoogleDriveBackupSyncService
    {
        public Task<bool> TryUploadAsync(BackupRecord backupRecord, BackupSettings settings, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }
    }
}
