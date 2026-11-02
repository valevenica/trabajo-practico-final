#nullable enable

using System;
using System.Linq;
using System.Threading.Tasks;
using StockManufactura.Application.Monetary;
using StockManufactura.Application.Resources;
using StockManufactura.Application.Services;
using StockManufactura.Domain.Entities;
using StockManufactura.UnitTests.TestDoubles;
using Xunit;

namespace StockManufactura.UnitTests;

public class BusinessIntelligenceServiceTests
{
    private const int Anio = 2026;
    private const int Mes = 8;

    [Fact]
    public async Task GetReportAsync_ValuesInventoryConvertingDollarResources()
    {
        var unitOfWork = new FakeUnitOfWork();
        unitOfWork.RecursosFake.Seed(
            new Recurso { Nombre = "Madera", StockActual = 10m, Precio = 500m, Moneda = Moneda.ARS, Activo = true },
            new Recurso { Nombre = "Resina", StockActual = 2m, Precio = 100m, Moneda = Moneda.USD, Activo = true });

        var report = await BuildService(unitOfWork).GetReportAsync(Anio, Mes);

        // 10 x 500 ARS + 2 x 100 USD x 1000 = 5.000 + 200.000
        Assert.Equal(205_000m, report.ValorInventarioInsumos);
        Assert.Equal(2, report.InsumosActivos);
    }

    [Fact]
    public async Task GetReportAsync_ExcludesServiceResourcesFromInventory()
    {
        var unitOfWork = new FakeUnitOfWork();
        unitOfWork.RecursosFake.Seed(
            new Recurso { Nombre = "Madera", StockActual = 10m, Precio = 500m, Activo = true },
            new Recurso { Nombre = "Electricidad", StockActual = 999m, Precio = 80m, Activo = true, EsServicio = true });

        var report = await BuildService(unitOfWork).GetReportAsync(Anio, Mes);

        Assert.Equal(1, report.InsumosActivos);
        Assert.Equal(5_000m, report.ValorInventarioInsumos);
    }

    [Fact]
    public async Task GetReportAsync_AveragesCostVariationOfThePeriod()
    {
        var unitOfWork = new FakeUnitOfWork();
        var producto = new Producto { Nombre = "Silla", Activo = true, CostoFabricacionActual = 1_200m };
        unitOfWork.ProductosFake.Seed(producto);
        unitOfWork.ProductCostHistoryFake.Seed(
            new ProductCostHistory { ProductId = producto.Id, Fecha = EnPeriodo(5), CostoAnterior = 1_000m, CostoNuevo = 1_100m },
            new ProductCostHistory { ProductId = producto.Id, Fecha = EnPeriodo(9), CostoAnterior = 1_000m, CostoNuevo = 1_300m });

        var report = await BuildService(unitOfWork).GetReportAsync(Anio, Mes);

        // Promedio de +10 % y +30 %.
        Assert.Equal(20m, report.VariacionCostoPromedioPct);
    }

    [Fact]
    public async Task GetReportAsync_NormalizesChartSeriesBetweenZeroAndOne()
    {
        var unitOfWork = new FakeUnitOfWork();
        unitOfWork.ProductosFake.Seed(
            new Producto { Nombre = "Caro", Activo = true, CostoFabricacionActual = 1_000m },
            new Producto { Nombre = "Barato", Activo = true, CostoFabricacionActual = 250m });

        var report = await BuildService(unitOfWork).GetReportAsync(Anio, Mes);

        Assert.Equal(2, report.CostoPorProducto.Count);
        Assert.Equal("Caro", report.CostoPorProducto[0].Etiqueta);
        Assert.Equal(1d, report.CostoPorProducto[0].Escala, 3);
        Assert.Equal(0.25d, report.CostoPorProducto[1].Escala, 3);
    }

    [Fact]
    public async Task GetReportAsync_RaisesAlertWhenProductsHaveNoCostHistory()
    {
        var unitOfWork = new FakeUnitOfWork();
        unitOfWork.ProductosFake.Seed(new Producto { Nombre = "Sin historial", Activo = true, CostoFabricacionActual = 500m });

        var report = await BuildService(unitOfWork).GetReportAsync(Anio, Mes);

        Assert.Contains(report.Alertas, alerta => alerta.Titulo.Contains("sin historial de costos"));
    }

    [Fact]
    public async Task GetReportAsync_RaisesCriticalAlertWhenThereAreShortages()
    {
        var faltantes = new ShortageReport
        {
            Insumos = new[]
            {
                new ResourceShortageRow { RecursoNombre = "Madera", Faltante = 5m }
            }
        };

        var report = await BuildService(new FakeUnitOfWork(), faltantes).GetReportAsync(Anio, Mes);

        var alerta = Assert.Single(report.Alertas, a => a.Severidad == "Critica");
        Assert.Contains("stock mínimo", alerta.Titulo);
    }

    [Fact]
    public async Task GetReportAsync_ReportsNoAlerts_WhenEverythingIsHealthy()
    {
        var report = await BuildService(new FakeUnitOfWork()).GetReportAsync(Anio, Mes);

        var alerta = Assert.Single(report.Alertas);
        Assert.Equal("Ok", alerta.Severidad);
    }

    [Fact]
    public async Task GetReportAsync_GroupsAuditActivityByModule()
    {
        var unitOfWork = new FakeUnitOfWork();
        unitOfWork.AuditLogsFake.Seed(
            new AuditLog { FechaHora = EnPeriodo(3), Modulo = "Productos", Usuario = "ana@test.com" },
            new AuditLog { FechaHora = EnPeriodo(4), Modulo = "Productos", Usuario = "juan@test.com" },
            new AuditLog { FechaHora = EnPeriodo(5), Modulo = "Backups", Usuario = "ana@test.com" });

        var report = await BuildService(unitOfWork).GetReportAsync(Anio, Mes);

        Assert.Equal("Productos", report.ActividadPorModulo[0].Etiqueta);
        Assert.Equal(2m, report.ActividadPorModulo[0].Valor);
        Assert.Contains("2 usuario(s)", report.ActividadPorModulo[0].Detalle);
    }

    [Fact]
    public async Task GetReportAsync_DescribesPeriod()
    {
        var service = BuildService(new FakeUnitOfWork());

        Assert.Equal("Historico completo", (await service.GetReportAsync(null, null)).PeriodoDescripcion);
        Assert.Equal("Agosto 2026", (await service.GetReportAsync(Anio, Mes)).PeriodoDescripcion);
    }

    [Fact]
    public async Task GetReportAsync_OrdenesPorEstado_NoDuplicaLosAliasDelEstado()
    {
        var unitOfWork = new FakeUnitOfWork();
        var finalizada = new OrdenProduccion("OP-3", Guid.NewGuid(), 1m, string.Empty);
        finalizada.Planificar();
        finalizada.MarcarEnProgreso();
        finalizada.RegistrarProduccion(1m);
        finalizada.Completar();
        unitOfWork.OrdenesProduccionFake.Seed(
            new OrdenProduccion("OP-1", Guid.NewGuid(), 1m, string.Empty),
            new OrdenProduccion("OP-2", Guid.NewGuid(), 1m, string.Empty),
            finalizada);

        var report = await BuildService(unitOfWork).GetReportAsync(Anio, Mes);

        Assert.Equal(new[] { "Borrador", "Finalizada" }, report.OrdenesPorEstado.Select(p => p.Etiqueta).ToArray());
        Assert.Equal(new[] { 2m, 1m }, report.OrdenesPorEstado.Select(p => p.Valor).ToArray());
    }

    private static DateTime EnPeriodo(int dia) => new(Anio, Mes, dia, 10, 0, 0, DateTimeKind.Utc);

    private static BusinessIntelligenceService BuildService(FakeUnitOfWork unitOfWork, ShortageReport? faltantes = null)
    {
        var monetario = new FakeMonetaryConfigurationService(new MonetaryConfigurationState
        {
            CurrentRate = 1_000m,
            LastUpdate = DateTime.UtcNow,
            Source = "DolarHoy"
        });

        return new BusinessIntelligenceService(unitOfWork, monetario, new FakeShortageService(faltantes));
    }
}
