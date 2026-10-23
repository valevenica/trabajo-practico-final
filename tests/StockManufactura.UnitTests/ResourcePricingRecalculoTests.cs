#nullable enable

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StockManufactura.Application.Resources;
using StockManufactura.Application.Services;
using StockManufactura.Domain.Entities;
using StockManufactura.Infrastructure.Db;
using StockManufactura.Infrastructure.Repositories;
using Xunit;

namespace StockManufactura.UnitTests;

/// <summary>
/// En la aplicación el servicio de precios y el de costos usan contextos distintos sobre la misma base. El recálculo debe
/// ver el precio ya guardado, tanto al cambiar un precio como al recalcular los insumos en dólares por una cotización nueva.
/// </summary>
public sealed class ResourcePricingRecalculoTests : IDisposable
{
    private readonly string _carpeta = Path.Combine(Path.GetTempPath(), "pricing-tests-" + Guid.NewGuid().ToString("N"));

    public ResourcePricingRecalculoTests() => Directory.CreateDirectory(_carpeta);

    [Fact]
    public async Task CambioDePrecio_YRecalculoUsd_ActualizanElCostoDelProducto()
    {
        var options = new DbContextOptionsBuilder<StockManufacturaDbContext>()
            .UseSqlite($"Data Source={Path.Combine(_carpeta, "base.db")};Pooling=False").Options;
        var cera = new Recurso { Codigo = "CERA", Nombre = "Cera", UnidadMedida = "kg", Precio = 100m, Activo = true };
        var aceite = new Recurso { Codigo = "ACE", Nombre = "Aceite", UnidadMedida = "ml", Precio = 1m, Moneda = Moneda.USD, Activo = true };
        var vela = new Producto { Codigo = "VELA", Nombre = "Vela", MargenActual = 0m };
        using (var setup = new StockManufacturaDbContext(options))
        {
            setup.Database.Migrate();
            setup.ExchangeRates.Add(new ExchangeRate { Valor = 1000m, EsPrioritaria = true });
            setup.Recursos.AddRange(cera, aceite);
            setup.Productos.Add(vela);
            setup.RecetaProductoItems.Add(new RecetaProductoItem { ProductoId = vela.Id, RecursoId = cera.Id, Cantidad = 2m });
            setup.RecetaProductoItems.Add(new RecetaProductoItem { ProductoId = vela.Id, RecursoId = aceite.Id, Cantidad = 1m });
            await setup.SaveChangesAsync();
        }

        using (var preciosContext = new StockManufacturaDbContext(options))
        using (var costosContext = new StockManufacturaDbContext(options))
        {
            var preciosUow = new UnitOfWork(preciosContext);
            var service = new ResourcePricingService(new ProductCostService(new UnitOfWork(costosContext)), new AuditLogService(preciosUow), preciosUow);

            await service.UpsertResourceAsync(new ResourceUpsertRequest
            {
                ResourceId = cera.Id, Codigo = "CERA", Nombre = "Cera", UnidadMedida = "kg", Precio = 150m, Moneda = Moneda.ARS,
                Activo = true, Usuario = "test", MotivoCambio = "Aumento"
            });
        }

        Assert.Equal(1_300m, await CostoAsync(options));    // 2 kg x 150 + 1 ml x USD 1 x 1.000

        using (var cotizacion = new StockManufacturaDbContext(options))
        {
            (await cotizacion.ExchangeRates.SingleAsync()).Valor = 1200m;
            await cotizacion.SaveChangesAsync();
        }

        using (var preciosContext = new StockManufacturaDbContext(options))
        using (var costosContext = new StockManufacturaDbContext(options))
        {
            var preciosUow = new UnitOfWork(preciosContext);
            var service = new ResourcePricingService(new ProductCostService(new UnitOfWork(costosContext)), new AuditLogService(preciosUow), preciosUow);
            await service.RecalcularTodosUSDAsync("test");
        }

        Assert.Equal(1_500m, await CostoAsync(options));    // 2 kg x 150 + 1 ml x USD 1 x 1.200
    }

    private static async Task<decimal> CostoAsync(DbContextOptions<StockManufacturaDbContext> options)
    {
        using var context = new StockManufacturaDbContext(options);
        return (await context.Productos.SingleAsync()).CostoFabricacionActual;
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
