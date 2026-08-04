#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using StockManufactura.Application.Interfaces;
using StockManufactura.Application.Monetary;
using StockManufactura.Application.Services;
using StockManufactura.Domain.Entities;
using Xunit;

namespace StockManufactura.UnitTests;

public class SystemStatusServiceTests
{
    [Fact]
    public async Task GetSnapshotAsync_MapsCountsAndLatestBackup()
    {
        var unitOfWork = new StubUnitOfWork();
        var backupService = new StubBackupService(new BackupRecord { FechaHora = new DateTime(2026, 8, 3, 12, 30, 0, DateTimeKind.Utc) });
        var monetaryService = new StubMonetaryConfigurationService(new MonetaryConfigurationState
        {
            CurrentRate = 1000m,
            LastUpdate = new DateTime(2026, 8, 3, 10, 0, 0, DateTimeKind.Utc),
            Source = "DolarHoy"
        });

        var service = new SystemStatusService(unitOfWork, monetaryService, backupService);

        var snapshot = await service.GetSnapshotAsync();

        Assert.Equal(2, snapshot.ProductCount);
        Assert.Equal(3, snapshot.ResourceCount);
        Assert.Equal(4, snapshot.CustomerCount);
        Assert.Equal(new DateTime(2026, 8, 3, 12, 30, 0, DateTimeKind.Utc), snapshot.LastBackupAt);
        Assert.Equal("DolarHoy", snapshot.LastDollarSource);
        Assert.True(snapshot.BackupEnabled);
        Assert.True(snapshot.DriveSyncEnabled);
        Assert.Equal("Activo", snapshot.BackupStatus);
        Assert.Equal("Google Drive", snapshot.CloudProvider);
    }

    private sealed class StubUnitOfWork : IUnitOfWork
    {
        public IRolRepository Roles { get; } = new StubRolRepository();
        public IUsuarioRepository Usuarios { get; } = new StubUsuarioRepository();
        public IProveedorRepository Proveedores { get; } = new StubProveedorRepository();
        public IRecursoRepository Recursos { get; } = new StubRecursoRepository();
        public IExchangeRateRepository ExchangeRates { get; } = new StubExchangeRateRepository();
        public IResourcePriceHistoryRepository ResourcePriceHistory { get; } = new StubResourcePriceHistoryRepository();
        public IProductoRepository Productos { get; } = new StubProductoRepository();
        public IOrdenProduccionRepository OrdenesProduccion { get; } = new StubOrdenProduccionRepository();
        public IRecetaProductoItemRepository RecetaProductoItems { get; } = new StubRecetaProductoItemRepository();
        public IStockRepository Stocks { get; } = new StubStockRepository();
        public IProductCostHistoryRepository ProductCostHistory { get; } = new StubProductCostHistoryRepository();
        public IProductCostSnapshotRepository ProductCostSnapshots { get; } = new StubProductCostSnapshotRepository();
        public IProductCostSnapshotItemRepository ProductCostSnapshotItems { get; } = new StubProductCostSnapshotItemRepository();
        public IAuditLogRepository AuditLogs { get; } = new StubAuditLogRepository();
        public IBackupRecordRepository BackupRecords { get; } = new StubBackupRecordRepository();
        public IBackupSettingsRepository BackupSettings { get; } = new StubBackupSettingsRepository();

        public Task<int> SaveChangesAsync() => Task.FromResult(0);
    }

    private class StubRepository<TEntity> : IRepository<TEntity> where TEntity : class
    {
        public Task<TEntity?> GetByIdAsync(Guid id) => Task.FromResult<TEntity?>(null);
        public virtual Task<IEnumerable<TEntity>> ListAsync() => Task.FromResult<IEnumerable<TEntity>>(Array.Empty<TEntity>());
        public Task AddAsync(TEntity entity) => Task.CompletedTask;
        public void Update(TEntity entity) { }
        public void Delete(TEntity entity) { }
    }

    private sealed class StubRolRepository : StubRepository<Rol>, IRolRepository
    {
    }

    private sealed class StubProveedorRepository : StubRepository<Proveedor>, IProveedorRepository
    {
        public Task<Proveedor?> GetByCuitAsync(string cuit) => Task.FromResult<Proveedor?>(null);
    }

    private sealed class StubExchangeRateRepository : StubRepository<ExchangeRate>, IExchangeRateRepository
    {
        public Task<ExchangeRate?> GetLatestAsync() => Task.FromResult<ExchangeRate?>(null);
    }

    private sealed class StubResourcePriceHistoryRepository : StubRepository<ResourcePriceHistory>, IResourcePriceHistoryRepository
    {
        public Task<IReadOnlyList<ResourcePriceHistory>> ListByResourceAsync(Guid recursoId) => Task.FromResult<IReadOnlyList<ResourcePriceHistory>>(Array.Empty<ResourcePriceHistory>());
    }

    private sealed class StubRecetaProductoItemRepository : StubRepository<RecetaProductoItem>, IRecetaProductoItemRepository
    {
        public Task<IReadOnlyList<RecetaProductoItem>> ListByProductIdAsync(Guid productId) => Task.FromResult<IReadOnlyList<RecetaProductoItem>>(Array.Empty<RecetaProductoItem>());
        public Task<IReadOnlyList<RecetaProductoItem>> ListByResourceIdAsync(Guid resourceId) => Task.FromResult<IReadOnlyList<RecetaProductoItem>>(Array.Empty<RecetaProductoItem>());
    }

    private sealed class StubStockRepository : StubRepository<Stock>, IStockRepository
    {
        public Task<Stock?> GetByProductoYUbicacionAsync(Guid productoId, Guid ubicacionId) => Task.FromResult<Stock?>(null);
        public Task<IReadOnlyList<Stock>> ListByProductoAsync(Guid productoId) => Task.FromResult<IReadOnlyList<Stock>>(Array.Empty<Stock>());
    }

    private sealed class StubProductCostHistoryRepository : StubRepository<ProductCostHistory>, IProductCostHistoryRepository
    {
        public Task<IReadOnlyList<ProductCostHistory>> ListByProductAsync(Guid productId) => Task.FromResult<IReadOnlyList<ProductCostHistory>>(Array.Empty<ProductCostHistory>());
    }

    private sealed class StubProductCostSnapshotRepository : StubRepository<ProductCostSnapshot>, IProductCostSnapshotRepository
    {
        public Task<IReadOnlyList<ProductCostSnapshot>> ListByProductAsync(Guid productId) => Task.FromResult<IReadOnlyList<ProductCostSnapshot>>(Array.Empty<ProductCostSnapshot>());
    }

    private sealed class StubProductCostSnapshotItemRepository : StubRepository<ProductCostSnapshotItem>, IProductCostSnapshotItemRepository
    {
        public Task<IReadOnlyList<ProductCostSnapshotItem>> ListBySnapshotAsync(Guid snapshotId) => Task.FromResult<IReadOnlyList<ProductCostSnapshotItem>>(Array.Empty<ProductCostSnapshotItem>());
    }

    private sealed class StubAuditLogRepository : StubRepository<AuditLog>, IAuditLogRepository
    {
        public Task<IReadOnlyList<AuditLog>> QueryAsync(DateTime? from, DateTime? to, string? usuario, string? modulo, string? accion) => Task.FromResult<IReadOnlyList<AuditLog>>(Array.Empty<AuditLog>());
    }

    private sealed class StubBackupRecordRepository : StubRepository<BackupRecord>, IBackupRecordRepository
    {
        public Task<IReadOnlyList<BackupRecord>> ListRecentAsync(int top) => Task.FromResult<IReadOnlyList<BackupRecord>>(Array.Empty<BackupRecord>());
    }

    private sealed class StubBackupSettingsRepository : StubRepository<BackupSettings>, IBackupSettingsRepository
    {
        public Task<BackupSettings?> GetCurrentAsync() => Task.FromResult<BackupSettings?>(null);
    }

    private sealed class StubProductoRepository : StubRepository<Producto>, IProductoRepository
    {
        public Task<Producto?> GetByCodigoAsync(string codigo) => Task.FromResult<Producto?>(null);
        public Task<IReadOnlyList<Producto>> ListActivosAsync() => Task.FromResult<IReadOnlyList<Producto>>(Array.Empty<Producto>());
        public override Task<IEnumerable<Producto>> ListAsync() => Task.FromResult<IEnumerable<Producto>>(new[]
        {
            new Producto(),
            new Producto()
        });
    }

    private sealed class StubOrdenProduccionRepository : StubRepository<OrdenProduccion>, IOrdenProduccionRepository
    {
        public Task<OrdenProduccion?> GetByCodigoAsync(string codigo) => Task.FromResult<OrdenProduccion?>(null);

        public Task<IReadOnlyList<OrdenProduccion>> ListByCreatedDescAsync()
            => Task.FromResult<IReadOnlyList<OrdenProduccion>>(Array.Empty<OrdenProduccion>());
    }

    private sealed class StubRecursoRepository : StubRepository<Recurso>, IRecursoRepository
    {
        public Task<Recurso?> GetByCodigoAsync(string codigo) => Task.FromResult<Recurso?>(null);
        public Task<IReadOnlyList<Recurso>> ListActivosAsync() => Task.FromResult<IReadOnlyList<Recurso>>(Array.Empty<Recurso>());
        public override Task<IEnumerable<Recurso>> ListAsync() => Task.FromResult<IEnumerable<Recurso>>(new[]
        {
            new Recurso(),
            new Recurso(),
            new Recurso()
        });
    }

    private sealed class StubUsuarioRepository : StubRepository<Usuario>, IUsuarioRepository
    {
        public override Task<IEnumerable<Usuario>> ListAsync() => Task.FromResult<IEnumerable<Usuario>>(new[]
        {
            new Usuario("Usuario 1", "user1@test.com", "hash", Guid.NewGuid()),
            new Usuario("Usuario 2", "user2@test.com", "hash", Guid.NewGuid()),
            new Usuario("Usuario 3", "user3@test.com", "hash", Guid.NewGuid()),
            new Usuario("Usuario 4", "user4@test.com", "hash", Guid.NewGuid())
        });

        public Task<Usuario?> GetByEmailAsync(string email, bool includeRole = false) => Task.FromResult<Usuario?>(null);

        public Task<Usuario?> GetByIdWithRoleAsync(Guid id) => Task.FromResult<Usuario?>(null);

        public Task<IReadOnlyList<Usuario>> ListWithRoleAsync() => Task.FromResult<IReadOnlyList<Usuario>>(Array.Empty<Usuario>());
    }

    private sealed class StubBackupService : IBackupService
    {
        private readonly BackupRecord? _latestBackup;

        public StubBackupService(BackupRecord? latestBackup)
        {
            _latestBackup = latestBackup;
        }

        public Task<BackupSettings> GetSettingsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new BackupSettings { GoogleDriveHabilitado = true });

        public Task SaveSettingsAsync(BackupSettings settings, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<BackupRecord> CreateManualBackupAsync(string usuario, CancellationToken cancellationToken = default) => Task.FromResult(new BackupRecord());

        public Task<BackupRecord> RestoreBackupAsync(string zipPath, string usuario, CancellationToken cancellationToken = default) => Task.FromResult(new BackupRecord());

        public Task<BackupRecord?> RunAutomaticBackupIfDueAsync(string usuario, CancellationToken cancellationToken = default) => Task.FromResult<BackupRecord?>(null);

        public Task<IReadOnlyList<BackupRecord>> GetRecentBackupsAsync(int top = 50, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<BackupRecord>>(_latestBackup is null ? Array.Empty<BackupRecord>() : new[] { _latestBackup });
    }

    private sealed class StubMonetaryConfigurationService : IMonetaryConfigurationService
    {
        private readonly MonetaryConfigurationState _state;

        public StubMonetaryConfigurationService(MonetaryConfigurationState state)
        {
            _state = state;
        }

        public IReadOnlyCollection<IExchangeRateProvider> GetProviders() => Array.Empty<IExchangeRateProvider>();

        public Task<MonetaryConfigurationState> GetCurrentStateAsync(CancellationToken cancellationToken = default) => Task.FromResult(_state);

        public Task<ExchangeRate> UpdateManualAsync(decimal value, string fuente, string usuario, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<ExchangeRate> UpdateAutomaticAsync(string providerKey, string usuario, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<IReadOnlyList<ExchangeRate>> GetHistoryAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ExchangeRate>>(Array.Empty<ExchangeRate>());
    }
}
