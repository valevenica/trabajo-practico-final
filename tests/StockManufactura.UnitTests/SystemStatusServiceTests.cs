#nullable enable

using System;
using System.Threading.Tasks;
using StockManufactura.Application.Monetary;
using StockManufactura.Application.Services;
using StockManufactura.Domain.Entities;
using StockManufactura.UnitTests.TestDoubles;
using Xunit;

namespace StockManufactura.UnitTests;

public class SystemStatusServiceTests
{
    private static readonly DateTime Backup = new(2026, 8, 3, 12, 30, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetSnapshotAsync_MapsCountsAndLatestBackup()
    {
        var unitOfWork = new FakeUnitOfWork();
        unitOfWork.ProductosFake.Seed(new Producto(), new Producto());
        unitOfWork.RecursosFake.Seed(new Recurso(), new Recurso(), new Recurso());
        unitOfWork.UsuariosFake.Seed(
            new Usuario("Usuario 1", "user1@test.com", "hash", Guid.NewGuid()),
            new Usuario("Usuario 2", "user2@test.com", "hash", Guid.NewGuid()),
            new Usuario("Usuario 3", "user3@test.com", "hash", Guid.NewGuid()),
            new Usuario("Usuario 4", "user4@test.com", "hash", Guid.NewGuid()));

        var service = BuildService(unitOfWork, new BackupSettings
        {
            GoogleDriveHabilitado = true,
            ProveedorNube = "Google Drive"
        });

        var snapshot = await service.GetSnapshotAsync();

        Assert.Equal(2, snapshot.ProductCount);
        Assert.Equal(3, snapshot.ResourceCount);
        Assert.Equal(4, snapshot.CustomerCount);
        Assert.Equal(Backup, snapshot.LastBackupAt);
        Assert.Equal("DolarHoy", snapshot.LastDollarSource);
        Assert.True(snapshot.BackupEnabled);
        Assert.True(snapshot.DriveSyncEnabled);
        Assert.Equal("Activo", snapshot.BackupStatus);
        Assert.Equal("Google Drive", snapshot.CloudProvider);
    }

    [Fact]
    public async Task GetSnapshotAsync_ReportsBidirectionalCloudSyncStatus()
    {
        var service = BuildService(new FakeUnitOfWork(), new BackupSettings
        {
            GoogleDriveHabilitado = true,
            SincronizacionBidireccional = true,
            UltimaSincronizacionNube = new DateTime(2026, 8, 3, 13, 0, 0, DateTimeKind.Utc)
        });

        var snapshot = await service.GetSnapshotAsync();

        Assert.True(snapshot.CloudBidirectionalEnabled);
        Assert.Equal("Bidireccional activa", snapshot.CloudSyncStatus);
        Assert.Equal(new DateTime(2026, 8, 3, 13, 0, 0, DateTimeKind.Utc), snapshot.LastDriveSyncAt);
    }

    [Fact]
    public async Task GetSnapshotAsync_ReportsUploadOnlyWhenBidirectionalIsDisabled()
    {
        var service = BuildService(new FakeUnitOfWork(), new BackupSettings
        {
            GoogleDriveHabilitado = true,
            SincronizacionBidireccional = false
        });

        var snapshot = await service.GetSnapshotAsync();

        Assert.False(snapshot.CloudBidirectionalEnabled);
        Assert.Equal("Solo subida", snapshot.CloudSyncStatus);
    }

    [Fact]
    public async Task GetSnapshotAsync_CountsAuditActivityInPeriod()
    {
        var unitOfWork = new FakeUnitOfWork();
        var enPeriodo = new DateTime(2026, 8, 10, 9, 0, 0, DateTimeKind.Utc);
        var fueraDePeriodo = new DateTime(2026, 7, 10, 9, 0, 0, DateTimeKind.Utc);

        unitOfWork.AuditLogsFake.Seed(
            new AuditLog { FechaHora = enPeriodo, Usuario = "ana@test.com", Modulo = "Productos", Accion = "Editar" },
            new AuditLog { FechaHora = enPeriodo, Usuario = "ana@test.com", Modulo = "Insumos", Accion = "Crear" },
            new AuditLog { FechaHora = enPeriodo, Usuario = "juan@test.com", Modulo = "Productos", Accion = "Crear" },
            new AuditLog { FechaHora = fueraDePeriodo, Usuario = "otro@test.com", Modulo = "Backups", Accion = "Crear" });

        var service = BuildService(unitOfWork, new BackupSettings());

        var snapshot = await service.GetSnapshotAsync(2026, 8);

        Assert.Equal(3, snapshot.AuditEventCount);
        Assert.Equal(2, snapshot.AuditUserCount);
        Assert.Equal(enPeriodo, snapshot.LastAuditAt);
        Assert.Equal("Productos · Editar", snapshot.LastAuditDescription);
    }

    [Fact]
    public async Task GetSnapshotAsync_ComputesTraceabilityCoverageOverActiveProducts()
    {
        var unitOfWork = new FakeUnitOfWork();
        var conHistorial = new Producto { Codigo = "P1", Activo = true };
        var sinHistorial = new Producto { Codigo = "P2", Activo = true };
        var inactivo = new Producto { Codigo = "P3", Activo = false };
        unitOfWork.ProductosFake.Seed(conHistorial, sinHistorial, inactivo);
        unitOfWork.ProductCostHistoryFake.Seed(new ProductCostHistory
        {
            ProductId = conHistorial.Id,
            Fecha = new DateTime(2026, 8, 5, 0, 0, 0, DateTimeKind.Utc),
            CostoAnterior = 100m,
            CostoNuevo = 120m
        });

        var service = BuildService(unitOfWork, new BackupSettings());

        var snapshot = await service.GetSnapshotAsync(2026, 8);

        // 1 de 2 productos activos tiene historial: el producto inactivo no cuenta.
        Assert.Equal(50m, snapshot.TraceabilityCoveragePercent);
        Assert.Equal(1, snapshot.CostRecalculationCount);
    }

    [Fact]
    public async Task GetSnapshotAsync_ComputesSalesIndicatorsForPeriod()
    {
        var unitOfWork = new FakeUnitOfWork();
        var producto = new Producto { Codigo = "P1", Activo = true, PrecioSugeridoActual = 100m, CostoFabricacionActual = 60m };
        unitOfWork.ProductosFake.Seed(producto);

        var finalizada1 = new OrdenProduccion("OP-1", producto.Id, 2m, string.Empty);
        finalizada1.Completar();
        var finalizada2 = new OrdenProduccion("OP-2", producto.Id, 3m, string.Empty);
        finalizada2.Completar();
        var enCurso = new OrdenProduccion("OP-3", producto.Id, 1m, string.Empty);
        var cancelada = new OrdenProduccion("OP-4", producto.Id, 5m, string.Empty);
        cancelada.Cancelar("prueba");
        unitOfWork.OrdenesProduccionFake.Seed(finalizada1, finalizada2, enCurso, cancelada);

        var service = BuildService(unitOfWork, new BackupSettings());
        var hoy = DateTime.UtcNow;

        var snapshot = await service.GetSnapshotAsync(hoy.Year, hoy.Month);

        // Ventas: (2 + 3) unidades x $100; costo: 5 x $60; la orden cancelada no cuenta como pedido.
        Assert.Equal(500m, snapshot.MonthlySalesTotal);
        Assert.Equal(300m, snapshot.MonthlySalesCost);
        Assert.Equal(40m, snapshot.GrossMarginPercent);
        Assert.Equal(250m, snapshot.AverageTicket);
        Assert.Equal(3, snapshot.OrdersInPeriodCount);
        Assert.Equal(1, snapshot.OpenOrdersCount);
        Assert.Equal(2, snapshot.FinishedOrdersInPeriodCount);
        Assert.Equal(0m, snapshot.PreviousPeriodSalesTotal);
        Assert.Null(snapshot.SalesVariationPercent);
    }

    private static SystemStatusService BuildService(FakeUnitOfWork unitOfWork, BackupSettings settings)
    {
        var backupService = new FakeBackupService(new BackupRecord { FechaHora = Backup }, settings);
        var monetaryService = new FakeMonetaryConfigurationService(new MonetaryConfigurationState
        {
            CurrentRate = 1000m,
            LastUpdate = new DateTime(2026, 8, 3, 10, 0, 0, DateTimeKind.Utc),
            Source = "DolarHoy"
        });

        return new SystemStatusService(
            unitOfWork,
            monetaryService,
            backupService,
            new FakeShortageService(),
            new FakeCloudBackupSyncService());
    }
}
