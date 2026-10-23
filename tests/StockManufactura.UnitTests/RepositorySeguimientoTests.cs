#nullable enable

using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StockManufactura.Domain.Entities;
using StockManufactura.Infrastructure.Db;
using StockManufactura.Infrastructure.Repositories;
using Xunit;

namespace StockManufactura.UnitTests;

/// <summary>
/// Reproduce el uso de una pantalla con contexto de larga vida: alta, recarga del listado sin seguimiento y ediciones
/// sucesivas sobre las copias recargadas.
/// </summary>
public class RepositorySeguimientoTests
{
    [Fact]
    public async Task CrearYEditarDosVeces_EnElMismoContexto_GuardaLaUltimaEdicion()
    {
        using var connection = new SqliteConnection("DataSource=:memory:;Foreign Keys=True");
        connection.Open();
        var options = new DbContextOptionsBuilder<StockManufacturaDbContext>().UseSqlite(connection).Options;
        using (var setup = new StockManufacturaDbContext(options))
        {
            setup.Database.Migrate();
        }

        using var context = new StockManufacturaDbContext(options);
        var unitOfWork = new UnitOfWork(context);
        await unitOfWork.Clientes.AddAsync(new Cliente { Nombre = "Cliente nuevo" });
        await unitOfWork.SaveChangesAsync();

        foreach (var nombre in new[] { "Primera edición", "Segunda edición" })
        {
            var copia = (await unitOfWork.Clientes.ListAsync()).Single();
            copia.Nombre = nombre;
            unitOfWork.Clientes.Update(copia);
            await unitOfWork.SaveChangesAsync();
        }

        var aBorrar = (await unitOfWork.Clientes.ListAsync()).Single();
        Assert.Equal("Segunda edición", aBorrar.Nombre);
        unitOfWork.Clientes.Delete(aBorrar);
        await unitOfWork.SaveChangesAsync();

        using var verificacion = new StockManufacturaDbContext(options);
        Assert.Empty(await verificacion.Clientes.ToListAsync());
    }
}
