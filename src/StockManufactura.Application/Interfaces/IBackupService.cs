using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Application.Interfaces
{
    public interface IBackupService
    {
        Task<BackupSettings> GetSettingsAsync(CancellationToken cancellationToken = default);
        Task SaveSettingsAsync(BackupSettings settings, CancellationToken cancellationToken = default);
        Task<BackupRecord> CreateManualBackupAsync(string usuario, CancellationToken cancellationToken = default);
        Task<BackupRecord> RestoreBackupAsync(string zipPath, string usuario, CancellationToken cancellationToken = default);
        Task<BackupRecord?> RunAutomaticBackupIfDueAsync(string usuario, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<BackupRecord>> GetRecentBackupsAsync(int top = 50, CancellationToken cancellationToken = default);
    }
}
