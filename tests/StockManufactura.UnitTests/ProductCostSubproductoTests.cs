#nullable enable

using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StockManufactura.Application.Products;
using StockManufactura.Application.Services;
using StockManufactura.Domain.Entities;
using StockManufactura.Infrastructure.Db;
using StockManufactura.Infrastructure.Repositories;
using Xunit;

namespace StockManufactura.UnitTests;

/// <summary>
/// Recálculo de costos con un producto que integra a otro como componente, sobre el ORM real: los dobles en memoria no
/// reproducen el seguimiento de entidades, que es donde aparecía el conflicto de instancias.
/// </summary>
public class ProductCostSubproductoTests
{
    [Fact]
    public async Task Recalcular_ProductoConSubproducto_NoFallaYUsaElCostoRecienCalculadoDelComponente()
    {
        using var connection = new SqliteConnection("DataSource=:memory:;Foreign Keys=True");
        connection.Open();
        var options = new DbContextOptionsBuilder<StockManufacturaDbContext>().UseSqlite(connection).Options;

        var cera = new Recurso { Codigo = "CERA", Nombre = "Cera", UnidadMedida = "kg", Precio = 100m, Activo = true };
        var vela = new Producto { Codigo = "VELA", Nombre = "Vela", MargenActual = 0.5m };
        var set = new Producto { Codigo = "SET", Nombre = "Set de dos velas", MargenActual = 0.5m };
        using (var setup = new StockManufacturaDbContext(options))
        {
            setup.Database.Migrate();
            setup.Recursos.Add(cera);
            setup.Productos.AddRange(vela, set);
            setup.RecetaProductoItems.Add(new RecetaProductoItem { ProductoId = vela.Id, RecursoId = cera.Id, Cantidad = 2m });
            setup.RecetaProductoItems.Add(new RecetaProductoItem { ProductoId = set.Id, ComponenteProductoId = vela.Id, Cantidad = 2m });
            await setup.SaveChangesAsync();
        }

        using (var context = new StockManufacturaDbContext(options))
        {
            var service = new ProductCostService(new UnitOfWork(context));
            await service.RecalculateAffectedProductsAsync(new ProductRecalculationRequest { Usuario = "test", Motivo = "alta", CambioReceta = true });
        }

        using (var precio = new StockManufacturaDbContext(options))
        {
            (await precio.Recursos.SingleAsync()).Precio = 150m;
            await precio.SaveChangesAsync();
        }

        using (var context = new StockManufacturaDbContext(options))
        {
            var service = new ProductCostService(new UnitOfWork(context));
            await service.RecalculateAffectedProductsAsync(new ProductRecalculationRequest { Usuario = "test", Motivo = "precio", RecursoDisparadorId = cera.Id });
        }

        using var verificacion = new StockManufacturaDbContext(options);
        var costos = await verificacion.Productos.ToDictionaryAsync(x => x.Codigo, x => x.CostoFabricacionActual);
        Assert.Equal(300m, costos["VELA"]);   // 2 kg x 150
        Assert.Equal(600m, costos["SET"]);    // 2 velas x 300, con el costo nuevo de la vela
    }

    [Fact]
    public async Task Recalcular_ConvierteLosInsumosEnDolaresConLaCotizacionPrioritaria()
    {
        using var connection = new SqliteConnection("DataSource=:memory:;Foreign Keys=True");
        connection.Open();
        var options = new DbContextOptionsBuilder<StockManufacturaDbContext>().UseSqlite(connection).Options;

        var aceite = new Recurso { Codigo = "ACE", Nombre = "Aceite importado", UnidadMedida = "ml", Precio = 2m, Moneda = Moneda.USD, Activo = true };
        var frasco = new Recurso { Codigo = "FRA", Nombre = "Frasco", UnidadMedida = "u", Precio = 300m, Moneda = Moneda.ARS, Activo = true };
        var vela = new Producto { Codigo = "VELA", Nombre = "Vela", MargenActual = 0.5m };
        using (var setup = new StockManufacturaDbContext(options))
        {
            setup.Database.Migrate();
            setup.ExchangeRates.Add(new ExchangeRate { Valor = 900m, Fuente = "Reciente", Fecha = System.DateTime.UtcNow });
            setup.ExchangeRates.Add(new ExchangeRate { Valor = 1000m, Fuente = "Prioritaria", EsPrioritaria = true, Fecha = System.DateTime.UtcNow.AddDays(-1) });
            setup.Recursos.AddRange(aceite, frasco);
            setup.Productos.Add(vela);
            setup.RecetaProductoItems.Add(new RecetaProductoItem { ProductoId = vela.Id, RecursoId = aceite.Id, Cantidad = 3m });
            setup.RecetaProductoItems.Add(new RecetaProductoItem { ProductoId = vela.Id, RecursoId = frasco.Id, Cantidad = 1m });
            await setup.SaveChangesAsync();
        }

        using (var context = new StockManufacturaDbContext(options))
        {
            var service = new ProductCostService(new UnitOfWork(context));
            await service.RecalculateAffectedProductsAsync(new ProductRecalculationRequest { Usuario = "test", Motivo = "alta", CambioReceta = true });
        }

        using var verificacion = new StockManufacturaDbContext(options);
        var producto = await verificacion.Productos.SingleAsync();
        Assert.Equal(6_300m, producto.CostoFabricacionActual);   // 3 ml x USD 2 x 1.000 + 1 frasco x $ 300
        Assert.Equal(9_450m, producto.PrecioSugeridoActual);
    }
}
