#nullable enable

using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StockManufactura.Application.Services;
using StockManufactura.Domain.Entities;
using StockManufactura.Infrastructure.Db;
using StockManufactura.Infrastructure.Repositories;
using Xunit;

namespace StockManufactura.UnitTests;

/// <summary>
/// La pantalla de ingreso mantiene un mismo contexto mientras está abierta. Estas pruebas usan una base SQLite
/// en memoria compartida por dos contextos para reproducir ese escenario, que los dobles en memoria no cubren.
/// </summary>
public class UsuarioRepositoryTrackingTests
{
    private const string Email = "operario@test.com";
    private const string NombreUsuario = "Operario";

    [Fact]
    public async Task AuthenticateDetailedAsync_AdmiteLaNuevaContrasenaRestablecidaDesdeOtroContexto()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<StockManufacturaDbContext>().UseSqlite(connection).Options;
        await SembrarOperarioAsync(options);

        using var loginContext = new StockManufacturaDbContext(options);
        var auth = new AuthenticationService(new UnitOfWork(loginContext));
        for (var i = 0; i < 5; i++)
        {
            await auth.AuthenticateDetailedAsync(NombreUsuario, "Incorrecta.1");
        }
        Assert.True((await auth.AuthenticateDetailedAsync(NombreUsuario, "Vieja.2026")).IsLockedOut);

        using (var recoveryContext = new StockManufacturaDbContext(options))
        {
            var usuario = await new UsuarioRepository(recoveryContext).GetByEmailAsync(Email);
            usuario!.CambiarPasswordHash(BCrypt.Net.BCrypt.HashPassword("Nueva.2026"));
            await recoveryContext.SaveChangesAsync();
        }

        var result = await auth.AuthenticateDetailedAsync(NombreUsuario, "Nueva.2026");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task GetByEmailAsync_ConservaLosCambiosPendientesDeLaEntidad()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<StockManufacturaDbContext>().UseSqlite(connection).Options;
        await SembrarOperarioAsync(options);

        using var context = new StockManufacturaDbContext(options);
        var repository = new UsuarioRepository(context);
        var usuario = await repository.GetByEmailAsync(Email);
        usuario!.ActualizarDatos("Nombre editado", Email);

        var mismo = await repository.GetByEmailAsync(Email);

        Assert.Equal("Nombre editado", mismo!.Nombre);
    }

    private static async Task SembrarOperarioAsync(DbContextOptions<StockManufacturaDbContext> options)
    {
        using var setup = new StockManufacturaDbContext(options);
        setup.Database.EnsureCreated();
        var rol = new Rol("Operador", "Operación diaria");
        setup.Roles.Add(rol);
        setup.Usuarios.Add(new Usuario(NombreUsuario, Email, BCrypt.Net.BCrypt.HashPassword("Vieja.2026"), rol.Id));
        await setup.SaveChangesAsync();
    }
}
