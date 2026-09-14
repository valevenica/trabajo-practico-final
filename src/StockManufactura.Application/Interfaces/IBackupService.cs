using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using StockManufactura.Application.Backups;
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

        /// <summary>
        /// Ejecuta una pasada de sincronizacion bidireccional contra el servicio en la nube configurado,
        /// dejando registro en la bitacora de auditoria y en el historial de copias.
        /// </summary>
        Task<CloudSyncResult> SynchronizeCloudAsync(string usuario, CancellationToken cancellationToken = default);
    }
}
