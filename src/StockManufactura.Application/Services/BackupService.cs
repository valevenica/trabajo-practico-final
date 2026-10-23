using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StockManufactura.Application.Backups;
using StockManufactura.Application.Interfaces;
using StockManufactura.Domain.Entities;
using StockManufactura.Shared;

namespace StockManufactura.Application.Services
{
    public sealed class BackupService : IBackupService
    {
        private readonly ICloudBackupSyncService _cloudSyncService;
        private readonly IAuditLogService _auditLogService;
        private readonly IUnitOfWork _unitOfWork;

        private readonly IDatabaseSnapshotService _databaseSnapshotService;

        // Copias y configuración se leen y escriben con los repositorios de la unidad de trabajo: inyectados por separado
        // usaban otro contexto y SaveChangesAsync de la unidad de trabajo nunca los persistía.
        public BackupService(
            ICloudBackupSyncService cloudSyncService,
            IAuditLogService auditLogService,
            IUnitOfWork unitOfWork,
            IDatabaseSnapshotService databaseSnapshotService)
        {
            _databaseSnapshotService = databaseSnapshotService;
            _cloudSyncService = cloudSyncService;
            _auditLogService = auditLogService;
            _unitOfWork = unitOfWork;
        }

        public async Task<BackupSettings> GetSettingsAsync(CancellationToken cancellationToken = default)
        {
            var settings = await _unitOfWork.BackupSettings.GetCurrentAsync();
            if (settings is not null)
            {
                return settings;
            }

            settings = new BackupSettings
            {
                CarpetaLocal = AppPaths.BackupsDirectory,
                Automatico = false,
                MantenerUltimasCopias = 10,
                IntervaloMinutos = 60
            };

            await _unitOfWork.BackupSettings.AddAsync(settings);
            await _unitOfWork.SaveChangesAsync();
            return settings;
        }

        public async Task SaveSettingsAsync(BackupSettings settings, CancellationToken cancellationToken = default)
        {
            _unitOfWork.BackupSettings.Update(settings);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<BackupRecord> CreateManualBackupAsync(string usuario, CancellationToken cancellationToken = default)
        {
            return await CreateBackupInternalAsync("Manual", usuario, cancellationToken);
        }

        public async Task<BackupRecord?> RunAutomaticBackupIfDueAsync(string usuario, CancellationToken cancellationToken = default)
        {
            var settings = await GetSettingsAsync(cancellationToken);
            if (!settings.Automatico)
            {
                return null;
            }

            if (settings.UltimoBackupAutomatico.HasValue)
            {
                var elapsed = DateTime.UtcNow - settings.UltimoBackupAutomatico.Value;
                if (elapsed.TotalMinutes < settings.IntervaloMinutos)
                {
                    return null;
                }
            }

            var record = await CreateBackupInternalAsync("Automatico", usuario, cancellationToken);
            settings.UltimoBackupAutomatico = DateTime.UtcNow;
            _unitOfWork.BackupSettings.Update(settings);
            await _unitOfWork.SaveChangesAsync();
            return record;
        }

        public async Task<BackupRecord> RestoreBackupAsync(string zipPath, string usuario, CancellationToken cancellationToken = default)
        {
            var restoredPath = Path.Combine(Path.GetTempPath(), $"stockmanufactura-restaurar-{Guid.NewGuid():N}.db");
            try
            {
                using (var archive = ZipFile.OpenRead(zipPath))
                {
                    var entry = archive.Entries.FirstOrDefault(x => x.Name.Equals("StockManufactura.db", StringComparison.OrdinalIgnoreCase));
                    if (entry is null)
                    {
                        throw new InvalidOperationException("El backup no contiene StockManufactura.db");
                    }

                    entry.ExtractToFile(restoredPath, overwrite: true);
                }

                // Se copia dentro de la base en uso con el motor, en lugar de sobrescribir el archivo abierto (que además
                // dejaría el WAL de la base anterior aplicado sobre la restaurada).
                await _databaseSnapshotService.ImportAsync(restoredPath, cancellationToken);
            }
            finally
            {
                if (File.Exists(restoredPath))
                {
                    File.Delete(restoredPath);
                }
            }

            var record = new BackupRecord
            {
                FechaHora = DateTime.UtcNow,
                Tipo = "Restauracion",
                RutaArchivo = zipPath,
                TamanoBytes = new FileInfo(zipPath).Length,
                Exitoso = true,
                Mensaje = $"Restaurado por {usuario}"
            };

            await _unitOfWork.BackupRecords.AddAsync(record);
            await _unitOfWork.SaveChangesAsync();
            await RegisterAuditAsync("Backups", "Restaurar", usuario, $"Restauracion desde {Path.GetFileName(zipPath)}", cancellationToken);
            return record;
        }

        public Task<IReadOnlyList<BackupRecord>> GetRecentBackupsAsync(int top = 50, CancellationToken cancellationToken = default)
        {
            return _unitOfWork.BackupRecords.ListRecentAsync(top);
        }

        public async Task<CloudSyncResult> SynchronizeCloudAsync(string usuario, CancellationToken cancellationToken = default)
        {
            var settings = await GetSettingsAsync(cancellationToken);
            var result = await _cloudSyncService.SynchronizeAsync(settings, cancellationToken);

            if (!result.Ejecutada)
            {
                return result;
            }

            // Cada copia traida desde la nube se incorpora al historial local para que quede
            // visible en la pantalla de backups y auditada como origen "Nube".
            foreach (var descargado in result.Descargados)
            {
                var rutaLocal = Path.Combine(settings.CarpetaLocal, descargado);
                await _unitOfWork.BackupRecords.AddAsync(new BackupRecord
                {
                    FechaHora = DateTime.UtcNow,
                    Tipo = "NubeDescarga",
                    RutaArchivo = rutaLocal,
                    TamanoBytes = File.Exists(rutaLocal) ? new FileInfo(rutaLocal).Length : 0,
                    Exitoso = true,
                    Mensaje = $"Descargado desde {result.Proveedor} por {usuario}"
                });
            }

            settings.UltimaSincronizacionNube = result.FechaUtc;
            _unitOfWork.BackupSettings.Update(settings);
            await _unitOfWork.SaveChangesAsync();

            await RegisterAuditAsync(
                "Backups",
                "SincronizarNube",
                usuario,
                $"{result.Mensaje} Proveedor: {result.Proveedor}.",
                cancellationToken);

            return result;
        }

        private async Task<BackupRecord> CreateBackupInternalAsync(string tipo, string usuario, CancellationToken cancellationToken)
        {
            var settings = await GetSettingsAsync(cancellationToken);
            Directory.CreateDirectory(settings.CarpetaLocal);

            var zipPath = Path.Combine(settings.CarpetaLocal, $"backup-{DateTime.UtcNow:yyyyMMdd-HHmmss}.zip");
            var snapshotPath = Path.Combine(Path.GetTempPath(), $"stockmanufactura-copia-{Guid.NewGuid():N}.db");

            try
            {
                // La base está abierta: se toma una copia consistente con el motor y se comprime esa copia.
                await _databaseSnapshotService.ExportAsync(snapshotPath, cancellationToken);
                using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);
                archive.CreateEntryFromFile(snapshotPath, "StockManufactura.db");
            }
            catch
            {
                // No se deja un .zip incompleto que luego parezca una copia válida.
                if (File.Exists(zipPath))
                {
                    File.Delete(zipPath);
                }

                throw;
            }
            finally
            {
                if (File.Exists(snapshotPath))
                {
                    File.Delete(snapshotPath);
                }
            }

            var record = new BackupRecord
            {
                FechaHora = DateTime.UtcNow,
                Tipo = tipo,
                RutaArchivo = zipPath,
                TamanoBytes = new FileInfo(zipPath).Length,
                Exitoso = true,
                Mensaje = $"Backup {tipo} generado por {usuario}"
            };

            await _unitOfWork.BackupRecords.AddAsync(record);
            await _unitOfWork.SaveChangesAsync();

            await TrySyncCloudAsync(record, settings, cancellationToken);
            ApplyRetention(settings);
            await RegisterAuditAsync("Backups", $"Backup{tipo}", usuario, $"Copia generada en {Path.GetFileName(zipPath)}", cancellationToken);

            return record;
        }

        private async Task TrySyncCloudAsync(BackupRecord record, BackupSettings settings, CancellationToken cancellationToken)
        {
            if (!settings.GoogleDriveHabilitado)
            {
                return;
            }

            var uploaded = await _cloudSyncService.TryUploadAsync(record, settings, cancellationToken);
            if (!uploaded)
            {
                return;
            }

            settings.UltimaSincronizacionNube = DateTime.UtcNow;
            _unitOfWork.BackupSettings.Update(settings);
            await _unitOfWork.SaveChangesAsync();
        }

        private async Task RegisterAuditAsync(string modulo, string accion, string usuario, string descripcion, CancellationToken cancellationToken)
        {
            try
            {
                await _auditLogService.RegisterAsync(new AuditLog
                {
                    FechaHora = DateTime.UtcNow,
                    Usuario = usuario,
                    Modulo = modulo,
                    Accion = accion,
                    Entidad = nameof(BackupRecord),
                    Descripcion = descripcion
                }, cancellationToken);
            }
            catch (Exception)
            {
                // La auditoria no debe impedir que la copia de seguridad se considere exitosa.
            }
        }

        private static void ApplyRetention(BackupSettings settings)
        {
            var files = Directory.GetFiles(settings.CarpetaLocal, "backup-*.zip")
                .Select(x => new FileInfo(x))
                .OrderByDescending(x => x.CreationTimeUtc)
                .ToList();

            foreach (var file in files.Skip(settings.MantenerUltimasCopias))
            {
                file.Delete();
            }
        }
    }
}
