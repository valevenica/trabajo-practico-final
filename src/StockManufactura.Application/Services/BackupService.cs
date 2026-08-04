using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StockManufactura.Application.Interfaces;
using StockManufactura.Domain.Entities;
using StockManufactura.Shared;

namespace StockManufactura.Application.Services
{
    public sealed class BackupService : IBackupService
    {
        private readonly IBackupRecordRepository _backupRecordRepository;
        private readonly IBackupSettingsRepository _backupSettingsRepository;
        private readonly IGoogleDriveBackupSyncService _googleDriveService;
        private readonly IUnitOfWork _unitOfWork;

        public BackupService(
            IBackupRecordRepository backupRecordRepository,
            IBackupSettingsRepository backupSettingsRepository,
            IGoogleDriveBackupSyncService googleDriveService,
            IUnitOfWork unitOfWork)
        {
            _backupRecordRepository = backupRecordRepository;
            _backupSettingsRepository = backupSettingsRepository;
            _googleDriveService = googleDriveService;
            _unitOfWork = unitOfWork;
        }

        public async Task<BackupSettings> GetSettingsAsync(CancellationToken cancellationToken = default)
        {
            var settings = await _backupSettingsRepository.GetCurrentAsync();
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

            await _backupSettingsRepository.AddAsync(settings);
            await _unitOfWork.SaveChangesAsync();
            return settings;
        }

        public async Task SaveSettingsAsync(BackupSettings settings, CancellationToken cancellationToken = default)
        {
            _backupSettingsRepository.Update(settings);
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
            _backupSettingsRepository.Update(settings);
            await _unitOfWork.SaveChangesAsync();
            return record;
        }

        public async Task<BackupRecord> RestoreBackupAsync(string zipPath, string usuario, CancellationToken cancellationToken = default)
        {
            var settings = await GetSettingsAsync(cancellationToken);
            var targetDbPath = AppPaths.DatabaseFilePath;
            Directory.CreateDirectory(Path.GetDirectoryName(targetDbPath)!);

            using var archive = ZipFile.OpenRead(zipPath);
            var entry = archive.Entries.FirstOrDefault(x => x.Name.Equals("StockManufactura.db", StringComparison.OrdinalIgnoreCase));
            if (entry is null)
            {
                throw new InvalidOperationException("El backup no contiene StockManufactura.db");
            }

            entry.ExtractToFile(targetDbPath, overwrite: true);

            var record = new BackupRecord
            {
                FechaHora = DateTime.UtcNow,
                Tipo = "Restauracion",
                RutaArchivo = zipPath,
                TamanoBytes = new FileInfo(zipPath).Length,
                Exitoso = true,
                Mensaje = $"Restaurado por {usuario}"
            };

            await _backupRecordRepository.AddAsync(record);
            await _unitOfWork.SaveChangesAsync();
            return record;
        }

        public Task<IReadOnlyList<BackupRecord>> GetRecentBackupsAsync(int top = 50, CancellationToken cancellationToken = default)
        {
            return _backupRecordRepository.ListRecentAsync(top);
        }

        private async Task<BackupRecord> CreateBackupInternalAsync(string tipo, string usuario, CancellationToken cancellationToken)
        {
            var settings = await GetSettingsAsync(cancellationToken);
            Directory.CreateDirectory(settings.CarpetaLocal);

            var dbPath = AppPaths.DatabaseFilePath;
            var zipPath = Path.Combine(settings.CarpetaLocal, $"backup-{DateTime.UtcNow:yyyyMMdd-HHmmss}.zip");

            using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                archive.CreateEntryFromFile(dbPath, "StockManufactura.db");
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

            await _backupRecordRepository.AddAsync(record);
            await _unitOfWork.SaveChangesAsync();

            await TrySyncGoogleDriveAsync(record, settings, cancellationToken);
            await ApplyRetentionAsync(settings);

            return record;
        }

        private async Task TrySyncGoogleDriveAsync(BackupRecord record, BackupSettings settings, CancellationToken cancellationToken)
        {
            if (!settings.GoogleDriveHabilitado)
            {
                return;
            }

            await _googleDriveService.TryUploadAsync(record, settings, cancellationToken);
        }

        private async Task ApplyRetentionAsync(BackupSettings settings)
        {
            var files = Directory.GetFiles(settings.CarpetaLocal, "backup-*.zip")
                .Select(x => new FileInfo(x))
                .OrderByDescending(x => x.CreationTimeUtc)
                .ToList();

            foreach (var file in files.Skip(settings.MantenerUltimasCopias))
            {
                file.Delete();
            }

            await Task.CompletedTask;
        }
    }
}
