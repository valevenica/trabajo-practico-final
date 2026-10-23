#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StockManufactura.Application.Backups;
using StockManufactura.Application.Interfaces;
using StockManufactura.Application.Services;
using StockManufactura.Domain.Entities;
using StockManufactura.Infrastructure.Backups;
using StockManufactura.Infrastructure.Db;
using StockManufactura.Infrastructure.Repositories;
using Xunit;

namespace StockManufactura.UnitTests;

/// <summary>
/// La configuración y el historial de copias deben quedar guardados en la base, no solo en la pantalla.
/// </summary>
public sealed class BackupServicePersistenciaTests : IDisposable
{
    private readonly string _carpeta = Path.Combine(Path.GetTempPath(), "backup-tests-" + Guid.NewGuid().ToString("N"));

    public BackupServicePersistenciaTests() => Directory.CreateDirectory(_carpeta);

    [Fact]
    public async Task GuardarConfiguracionYCopiaManual_QuedanPersistidas()
    {
        var basePath = Path.Combine(_carpeta, "base.db");
        var options = new DbContextOptionsBuilder<StockManufacturaDbContext>().UseSqlite($"Data Source={basePath};Pooling=False").Options;
        using (var setup = new StockManufacturaDbContext(options))
        {
            setup.Database.Migrate();
        }

        using (var context = new StockManufacturaDbContext(options))
        {
            var unitOfWork = new UnitOfWork(context);
            var service = new BackupService(new SinNube(), new AuditLogService(unitOfWork), unitOfWork, new SqliteDatabaseSnapshotService(basePath));

            var settings = await service.GetSettingsAsync();
            settings.CarpetaLocal = Path.Combine(_carpeta, "copias");
            settings.CarpetaNube = Path.Combine(_carpeta, "nube");
            settings.SincronizacionBidireccional = true;
            await service.SaveSettingsAsync(settings);

            await service.CreateManualBackupAsync("tester");
        }

        using var verificacion = new StockManufacturaDbContext(options);
        var guardada = await verificacion.BackupSettings.SingleAsync();
        Assert.Equal(Path.Combine(_carpeta, "nube"), guardada.CarpetaNube);
        Assert.True(guardada.SincronizacionBidireccional);
        var copia = await verificacion.BackupRecords.SingleAsync();
        Assert.True(File.Exists(copia.RutaArchivo));
        Assert.True(copia.TamanoBytes > 0);
    }

    private sealed class SinNube : ICloudBackupSyncService
    {
        public string ProviderName => "Sin nube";

        public Task<bool> IsAvailableAsync(BackupSettings settings, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task<bool> TryUploadAsync(BackupRecord backupRecord, BackupSettings settings, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task<IReadOnlyList<CloudBackupItem>> ListRemoteAsync(BackupSettings settings, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<CloudBackupItem>>(Array.Empty<CloudBackupItem>());

        public Task<CloudSyncResult> SynchronizeAsync(BackupSettings settings, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try
        {
            Directory.Delete(_carpeta, recursive: true);
        }
        catch (IOException)
        {
        }
    }
}
