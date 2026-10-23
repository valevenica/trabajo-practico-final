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
/// La pantalla de insumos agrega, prioriza y quita proveedores de un insumo sobre un mismo contexto, que se reproduce acá
/// con el ORM real sobre SQLite en memoria.
/// </summary>
public class RecursoProveedorTrackingTests
{
    [Fact]
    public async Task AgregarYLuegoPriorizarYQuitar_EnElMismoContexto_NoProducenConflictoDeInstancias()
    {
        using var connection = new SqliteConnection("DataSource=:memory:;Foreign Keys=True");
        connection.Open();
        var options = new DbContextOptionsBuilder<StockManufacturaDbContext>().UseSqlite(connection).Options;
        var recurso = new Recurso { Codigo = "ACE", Nombre = "Aceite", UnidadMedida = "ml", Activo = true };
        var principal = new Proveedor { Nombre = "Principal", RazonSocial = "Principal SA", Cuit = "30-1" };
        var alternativo = new Proveedor { Nombre = "Alternativo", RazonSocial = "Alternativo SA", Cuit = "30-2" };
        using (var setup = new StockManufacturaDbContext(options))
        {
            setup.Database.Migrate();
            setup.Recursos.Add(recurso);
            setup.Proveedores.AddRange(principal, alternativo);
            setup.RecursoProveedores.Add(new RecursoProveedor { RecursoId = recurso.Id, ProveedorId = principal.Id, Precio = 10m, EsPrioritario = true });
            await setup.SaveChangesAsync();
        }

        using var context = new StockManufacturaDbContext(options);
        var unitOfWork = new UnitOfWork(context);
        var nuevo = new RecursoProveedor { RecursoId = recurso.Id, ProveedorId = alternativo.Id, Precio = 9m, CodigoProveedor = "ALT-1" };
        await unitOfWork.RecursoProveedores.AddAsync(nuevo);
        await unitOfWork.SaveChangesAsync();

        foreach (var item in await unitOfWork.RecursoProveedores.ListByRecursoIdAsync(recurso.Id))
        {
            item.EsPrioritario = item.Id == nuevo.Id;
            unitOfWork.RecursoProveedores.Update(item);
        }
        await unitOfWork.SaveChangesAsync();

        var aQuitar = (await unitOfWork.RecursoProveedores.ListByRecursoIdAsync(recurso.Id)).Single(x => x.Id == nuevo.Id);
        unitOfWork.RecursoProveedores.Delete(aQuitar);
        await unitOfWork.SaveChangesAsync();

        using var verificacion = new StockManufacturaDbContext(options);
        var restante = await verificacion.RecursoProveedores.SingleAsync();
        Assert.Equal(principal.Id, restante.ProveedorId);
    }
}
