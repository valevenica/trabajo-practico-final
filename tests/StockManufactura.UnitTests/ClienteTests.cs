#nullable enable

using System;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StockManufactura.Domain.Entities;
using StockManufactura.Infrastructure.Db;
using Xunit;

namespace StockManufactura.UnitTests;

/// <summary>
/// Módulo de clientes: el cliente del pedido es opcional (consumidor final) y el documento es único solo cuando se informa.
/// Las pruebas de persistencia aplican todas las migraciones sobre una base SQLite en memoria.
/// </summary>
public class ClienteTests
{
    [Fact]
    public void OrdenProduccion_SinCliente_EsVentaAConsumidorFinal()
    {
        var orden = new OrdenProduccion("OP-1", Guid.NewGuid(), 10m, string.Empty);

        Assert.Null(orden.ClienteId);
    }

    [Fact]
    public void AsignarCliente_PermiteCambiarloMientrasElPedidoEstaAbierto()
    {
        var orden = new OrdenProduccion("OP-1", Guid.NewGuid(), 10m, string.Empty);
        var clienteId = Guid.NewGuid();

        orden.AsignarCliente(clienteId);
        orden.Planificar();
        orden.AsignarCliente(null);

        Assert.Null(orden.ClienteId);
    }

    [Fact]
    public void AsignarCliente_RechazaPedidosFinalizados()
    {
        var orden = new OrdenProduccion("OP-1", Guid.NewGuid(), 10m, string.Empty);
        orden.Planificar();
        orden.MarcarEnProgreso();
        orden.RegistrarProduccion(10m);
        orden.Completar();

        Assert.Throws<InvalidOperationException>(() => orden.AsignarCliente(Guid.NewGuid()));
    }

    [Fact]
    public async Task Documento_EsUnicoSoloCuandoSeInforma()
    {
        using var connection = AbrirBase(out var options);
        using var context = new StockManufacturaDbContext(options);
        context.Clientes.Add(new Cliente { Nombre = "Consumidor A" });
        context.Clientes.Add(new Cliente { Nombre = "Consumidor B" });
        context.Clientes.Add(new Cliente { Nombre = "Taller Norte", Documento = "30-11111111-1" });
        await context.SaveChangesAsync();

        context.Clientes.Add(new Cliente { Nombre = "Otro", Documento = "30-11111111-1" });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Pedido_GuardaSuCliente_YElClienteConPedidosNoSePuedeEliminar()
    {
        using var connection = AbrirBase(out var options);
        Guid clienteId;
        using (var context = new StockManufacturaDbContext(options))
        {
            var producto = new Producto { Codigo = "PRD-1", Nombre = "Vela", Descripcion = "Vela de prueba" };
            var cliente = new Cliente { Nombre = "Boutique Centro", Documento = "27-22222222-2" };
            context.Productos.Add(producto);
            context.Clientes.Add(cliente);
            await context.SaveChangesAsync();
            context.OrdenesProduccion.Add(new OrdenProduccion("OP-1", producto.Id, 5m, string.Empty, cliente.Id));
            await context.SaveChangesAsync();
            clienteId = cliente.Id;
        }

        using (var verificacion = new StockManufacturaDbContext(options))
        {
            var orden = await verificacion.OrdenesProduccion.SingleAsync();
            Assert.Equal(clienteId, orden.ClienteId);
        }

        using var borrado = new StockManufacturaDbContext(options);
        borrado.Clientes.Remove(await borrado.Clientes.SingleAsync());
        await Assert.ThrowsAsync<DbUpdateException>(() => borrado.SaveChangesAsync());
    }

    private static SqliteConnection AbrirBase(out DbContextOptions<StockManufacturaDbContext> options)
    {
        // Con la conexión abierta desde afuera, EF no activa por sí mismo las claves foráneas de SQLite.
        var connection = new SqliteConnection("DataSource=:memory:;Foreign Keys=True");
        connection.Open();
        options = new DbContextOptionsBuilder<StockManufacturaDbContext>().UseSqlite(connection).Options;
        using var context = new StockManufacturaDbContext(options);
        context.Database.Migrate();
        return connection;
    }
}
