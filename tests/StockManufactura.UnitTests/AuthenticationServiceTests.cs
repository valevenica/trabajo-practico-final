#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StockManufactura.Application.Interfaces;
using StockManufactura.Application.Services;
using StockManufactura.Domain.Entities;
using Xunit;

namespace StockManufactura.UnitTests;

public class AuthenticationServiceTests
{
    [Fact]
    public async Task AuthenticateAsync_ReturnsUser_WhenCredentialsAreValid()
    {
        var rol = new Rol("Administrador", "Rol administrador");
        var usuario = new Usuario("Admin", "admin@test.com", BCrypt.Net.BCrypt.HashPassword("Secret123!"), rol.Id);
        usuario.AsignarRol(rol);

        var unitOfWork = new StubUnitOfWork(usuario);
        var service = new AuthenticationService(unitOfWork);

        var result = await service.AuthenticateAsync("admin@test.com", "Secret123!");

        Assert.NotNull(result);
        Assert.Equal("admin@test.com", result!.Email);
    }

    [Fact]
    public async Task RegisterLoginAsync_UpdatesLastAccess()
    {
        var rol = new Rol("Administrador", "Rol administrador");
        var usuario = new Usuario("Admin", "admin@test.com", BCrypt.Net.BCrypt.HashPassword("Secret123!"), rol.Id);
        usuario.AsignarRol(rol);

        var unitOfWork = new StubUnitOfWork(usuario);
        var service = new AuthenticationService(unitOfWork);

        var result = await service.RegisterLoginAsync(usuario);

        Assert.NotNull(result.UltimoAcceso);
        Assert.True(unitOfWork.SaveCalled);
    }

    [Fact]
    public async Task AuthenticateDetailedAsync_BlocksUserAfterMaxFailedAttempts()
    {
        var rol = new Rol("Administrador", "Rol administrador");
        var usuario = new Usuario("Admin", "admin@test.com", BCrypt.Net.BCrypt.HashPassword("Secret123!"), rol.Id);
        usuario.AsignarRol(rol);

        var unitOfWork = new StubUnitOfWork(usuario);
        var service = new AuthenticationService(unitOfWork);

        for (var i = 0; i < 5; i++)
        {
            await service.AuthenticateDetailedAsync("admin@test.com", "incorrecta");
        }

        var lockedResult = await service.AuthenticateDetailedAsync("admin@test.com", "Secret123!");
        Assert.False(lockedResult.IsSuccess);
        Assert.True(lockedResult.IsLockedOut);
    }

    private sealed class StubUnitOfWork : IUnitOfWork
    {
        private readonly Usuario? _usuario;

        public StubUnitOfWork(Usuario? usuario = null)
        {
            _usuario = usuario;
            Usuarios = new StubUsuarioRepository(usuario);
        }

        public bool SaveCalled { get; private set; }

        public IRolRepository Roles { get; } = new StubRolRepository();
        public IUsuarioRepository Usuarios { get; }
        public IProveedorRepository Proveedores { get; } = new StubProveedorRepository();
        public IRecursoRepository Recursos { get; } = new StubRecursoRepository();
        public IExchangeRateRepository ExchangeRates { get; } = new StubExchangeRateRepository();
        public IResourcePriceHistoryRepository ResourcePriceHistory { get; } = new StubResourcePriceHistoryRepository();
        public IProductoRepository Productos { get; } = new StubProductoRepository();
        public IRecetaProductoItemRepository RecetaProductoItems { get; } = new StubRecetaProductoItemRepository();
        public IStockRepository Stocks { get; } = new StubStockRepository();
        public IProductCostHistoryRepository ProductCostHistory { get; } = new StubProductCostHistoryRepository();
        public IProductCostSnapshotRepository ProductCostSnapshots { get; } = new StubProductCostSnapshotRepository();
        public IProductCostSnapshotItemRepository ProductCostSnapshotItems { get; } = new StubProductCostSnapshotItemRepository();
        public IAuditLogRepository AuditLogs { get; } = new StubAuditLogRepository();
        public IBackupRecordRepository BackupRecords { get; } = new StubBackupRecordRepository();
        public IBackupSettingsRepository BackupSettings { get; } = new StubBackupSettingsRepository();

        public Task<int> SaveChangesAsync()
        {
            SaveCalled = true;
            return Task.FromResult(0);
        }
    }

    private sealed class StubUsuarioRepository : StubRepository<Usuario>, IUsuarioRepository
    {
        private readonly Usuario? _usuario;

        public StubUsuarioRepository(Usuario? usuario = null)
        {
            _usuario = usuario;
        }

        public override Task<IEnumerable<Usuario>> ListAsync() => Task.FromResult<IEnumerable<Usuario>>(new[] { _usuario! });

        public Task<Usuario?> GetByEmailAsync(string email, bool includeRole = false)
        {
            if (_usuario is not null && _usuario.Email.Equals(email, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult<Usuario?>(_usuario);
            }

            return Task.FromResult<Usuario?>(null);
        }

        public Task<Usuario?> GetByIdWithRoleAsync(Guid id)
        {
            return Task.FromResult<Usuario?>(_usuario?.Id == id ? _usuario : null);
        }

        public Task<IReadOnlyList<Usuario>> ListWithRoleAsync()
        {
            IReadOnlyList<Usuario> users = _usuario is null ? Array.Empty<Usuario>() : new[] { _usuario };
            return Task.FromResult(users);
        }
    }

    private class StubRepository<TEntity> : IRepository<TEntity> where TEntity : class
    {
        public Task<TEntity?> GetByIdAsync(Guid id) => Task.FromResult<TEntity?>(null);
        public virtual Task<IEnumerable<TEntity>> ListAsync() => Task.FromResult<IEnumerable<TEntity>>(Array.Empty<TEntity>());
        public Task AddAsync(TEntity entity) => Task.CompletedTask;
        public void Update(TEntity entity) { }
        public void Delete(TEntity entity) { }
    }

    private sealed class StubRolRepository : StubRepository<Rol>, IRolRepository { }
    private sealed class StubProveedorRepository : StubRepository<Proveedor>, IProveedorRepository { public Task<Proveedor?> GetByCuitAsync(string cuit) => Task.FromResult<Proveedor?>(null); }
    private sealed class StubRecursoRepository : StubRepository<Recurso>, IRecursoRepository { public Task<Recurso?> GetByCodigoAsync(string codigo) => Task.FromResult<Recurso?>(null); public Task<IReadOnlyList<Recurso>> ListActivosAsync() => Task.FromResult<IReadOnlyList<Recurso>>(Array.Empty<Recurso>()); }
    private sealed class StubExchangeRateRepository : StubRepository<ExchangeRate>, IExchangeRateRepository { public Task<ExchangeRate?> GetLatestAsync() => Task.FromResult<ExchangeRate?>(null); }
    private sealed class StubResourcePriceHistoryRepository : StubRepository<ResourcePriceHistory>, IResourcePriceHistoryRepository { public Task<IReadOnlyList<ResourcePriceHistory>> ListByResourceAsync(Guid recursoId) => Task.FromResult<IReadOnlyList<ResourcePriceHistory>>(Array.Empty<ResourcePriceHistory>()); }
    private sealed class StubProductoRepository : StubRepository<Producto>, IProductoRepository { public Task<Producto?> GetByCodigoAsync(string codigo) => Task.FromResult<Producto?>(null); public Task<IReadOnlyList<Producto>> ListActivosAsync() => Task.FromResult<IReadOnlyList<Producto>>(Array.Empty<Producto>()); }
    private sealed class StubRecetaProductoItemRepository : StubRepository<RecetaProductoItem>, IRecetaProductoItemRepository { public Task<IReadOnlyList<RecetaProductoItem>> ListByProductIdAsync(Guid productId) => Task.FromResult<IReadOnlyList<RecetaProductoItem>>(Array.Empty<RecetaProductoItem>()); public Task<IReadOnlyList<RecetaProductoItem>> ListByResourceIdAsync(Guid resourceId) => Task.FromResult<IReadOnlyList<RecetaProductoItem>>(Array.Empty<RecetaProductoItem>()); }
    private sealed class StubStockRepository : StubRepository<Stock>, IStockRepository { public Task<Stock?> GetByProductoYUbicacionAsync(Guid productoId, Guid ubicacionId) => Task.FromResult<Stock?>(null); public Task<IReadOnlyList<Stock>> ListByProductoAsync(Guid productoId) => Task.FromResult<IReadOnlyList<Stock>>(Array.Empty<Stock>()); }
    private sealed class StubProductCostHistoryRepository : StubRepository<ProductCostHistory>, IProductCostHistoryRepository { public Task<IReadOnlyList<ProductCostHistory>> ListByProductAsync(Guid productId) => Task.FromResult<IReadOnlyList<ProductCostHistory>>(Array.Empty<ProductCostHistory>()); }
    private sealed class StubProductCostSnapshotRepository : StubRepository<ProductCostSnapshot>, IProductCostSnapshotRepository { public Task<IReadOnlyList<ProductCostSnapshot>> ListByProductAsync(Guid productId) => Task.FromResult<IReadOnlyList<ProductCostSnapshot>>(Array.Empty<ProductCostSnapshot>()); }
    private sealed class StubProductCostSnapshotItemRepository : StubRepository<ProductCostSnapshotItem>, IProductCostSnapshotItemRepository { public Task<IReadOnlyList<ProductCostSnapshotItem>> ListBySnapshotAsync(Guid snapshotId) => Task.FromResult<IReadOnlyList<ProductCostSnapshotItem>>(Array.Empty<ProductCostSnapshotItem>()); }
    private sealed class StubAuditLogRepository : StubRepository<AuditLog>, IAuditLogRepository { public Task<IReadOnlyList<AuditLog>> QueryAsync(DateTime? from, DateTime? to, string? usuario, string? modulo, string? accion) => Task.FromResult<IReadOnlyList<AuditLog>>(Array.Empty<AuditLog>()); }
    private sealed class StubBackupRecordRepository : StubRepository<BackupRecord>, IBackupRecordRepository { public Task<IReadOnlyList<BackupRecord>> ListRecentAsync(int top) => Task.FromResult<IReadOnlyList<BackupRecord>>(Array.Empty<BackupRecord>()); }
    private sealed class StubBackupSettingsRepository : StubRepository<BackupSettings>, IBackupSettingsRepository { public Task<BackupSettings?> GetCurrentAsync() => Task.FromResult<BackupSettings?>(null); }
}
