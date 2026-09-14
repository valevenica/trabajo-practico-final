using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using StockManufactura.Application.Backups;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Application.Interfaces
{
    /// <summary>
    /// Contrato de sincronizacion de copias de seguridad contra un servicio en la nube.
    /// Se define en Application para que la capa de aplicacion no dependa de un proveedor concreto:
    /// cualquier implementacion (carpeta replicada por el cliente de la nube, API REST de Drive,
    /// OneDrive, S3) se puede sustituir sin tocar los casos de uso.
    /// </summary>
    public interface ICloudBackupSyncService
    {
        string ProviderName { get; }

        /// <summary>Indica si el destino en la nube esta configurado y accesible en este momento.</summary>
        Task<bool> IsAvailableAsync(BackupSettings settings, CancellationToken cancellationToken = default);

        /// <summary>Sube una copia puntual recien generada. Devuelve false si no pudo subirla.</summary>
        Task<bool> TryUploadAsync(BackupRecord backupRecord, BackupSettings settings, CancellationToken cancellationToken = default);

        /// <summary>Lista las copias existentes del lado de la nube.</summary>
        Task<IReadOnlyList<CloudBackupItem>> ListRemoteAsync(BackupSettings settings, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sincroniza en ambos sentidos: sube las copias locales ausentes en la nube y descarga
        /// las copias remotas ausentes en el equipo, reportando los conflictos detectados.
        /// </summary>
        Task<CloudSyncResult> SynchronizeAsync(BackupSettings settings, CancellationToken cancellationToken = default);
    }
}
