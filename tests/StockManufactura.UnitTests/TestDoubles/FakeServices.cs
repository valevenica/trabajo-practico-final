#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using StockManufactura.Application.Backups;
using StockManufactura.Application.Interfaces;
using StockManufactura.Application.Monetary;
using StockManufactura.Application.Resources;
using StockManufactura.Domain.Entities;

namespace StockManufactura.UnitTests.TestDoubles;

public sealed class FakeBackupService : IBackupService
{
    private readonly BackupRecord? _latestBackup;
    private readonly BackupSettings _settings;

    public FakeBackupService(BackupRecord? latestBackup = null, BackupSettings? settings = null)
    {
        _latestBackup = latestBackup;
        _settings = settings ?? new BackupSettings { GoogleDriveHabilitado = true };
    }

    public Task<BackupSettings> GetSettingsAsync(CancellationToken cancellationToken = default) => Task.FromResult(_settings);

    public Task SaveSettingsAsync(BackupSettings settings, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<BackupRecord> CreateManualBackupAsync(string usuario, CancellationToken cancellationToken = default)
        => Task.FromResult(new BackupRecord());

    public Task<BackupRecord> RestoreBackupAsync(string zipPath, string usuario, CancellationToken cancellationToken = default)
        => Task.FromResult(new BackupRecord());

    public Task<BackupRecord?> RunAutomaticBackupIfDueAsync(string usuario, CancellationToken cancellationToken = default)
        => Task.FromResult<BackupRecord?>(null);

    public Task<IReadOnlyList<BackupRecord>> GetRecentBackupsAsync(int top = 50, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<BackupRecord>>(_latestBackup is null ? Array.Empty<BackupRecord>() : new[] { _latestBackup });

    public Task<CloudSyncResult> SynchronizeCloudAsync(string usuario, CancellationToken cancellationToken = default)
        => Task.FromResult(CloudSyncResult.NoEjecutada("Sin nube en pruebas."));
}

public sealed class FakeMonetaryConfigurationService : IMonetaryConfigurationService
{
    private readonly MonetaryConfigurationState _state;

    public FakeMonetaryConfigurationService(MonetaryConfigurationState state)
    {
        _state = state;
    }

    public IReadOnlyCollection<IExchangeRateProvider> GetProviders() => Array.Empty<IExchangeRateProvider>();

    public Task<MonetaryConfigurationState> GetCurrentStateAsync(CancellationToken cancellationToken = default) => Task.FromResult(_state);

    public Task<ExchangeRate> UpdateManualAsync(decimal value, string fuente, string usuario, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task<ExchangeRate> UpdateAutomaticAsync(string providerKey, string usuario, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task<IReadOnlyList<ExchangeRate>> GetHistoryAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ExchangeRate>>(Array.Empty<ExchangeRate>());

    public Task SetPrioritariaAsync(Guid id, string usuario, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

public sealed class FakeShortageService : IShortageService
{
    private readonly ShortageReport _report;

    public FakeShortageService(ShortageReport? report = null)
    {
        _report = report ?? new ShortageReport();
    }

    public Task<ShortageReport> GetShortagesAsync(CancellationToken cancellationToken = default) => Task.FromResult(_report);
}

public sealed class FakeCloudBackupSyncService : ICloudBackupSyncService
{
    private readonly bool _available;

    public FakeCloudBackupSyncService(bool available = true)
    {
        _available = available;
    }

    public string ProviderName => "Nube de prueba";

    public Task<bool> IsAvailableAsync(BackupSettings settings, CancellationToken cancellationToken = default)
        => Task.FromResult(_available);

    public Task<bool> TryUploadAsync(BackupRecord backupRecord, BackupSettings settings, CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task<IReadOnlyList<CloudBackupItem>> ListRemoteAsync(BackupSettings settings, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<CloudBackupItem>>(Array.Empty<CloudBackupItem>());

    public Task<CloudSyncResult> SynchronizeAsync(BackupSettings settings, CancellationToken cancellationToken = default)
        => Task.FromResult(new CloudSyncResult { Ejecutada = true, Proveedor = ProviderName });
}
