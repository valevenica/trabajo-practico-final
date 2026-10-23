#nullable enable

using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging.Abstractions;
using StockManufactura.Application.DTOs;
using StockManufactura.Application.Mappings;
using StockManufactura.Application.Services;
using StockManufactura.Domain.Entities;
using StockManufactura.Infrastructure.Db;
using StockManufactura.UnitTests.TestDoubles;
using Xunit;

namespace StockManufactura.UnitTests;

/// <summary>
/// El nombre de usuario es la credencial de ingreso: no puede repetirse y el correo no sirve para ingresar.
/// </summary>
public class NombreUsuarioUnicoTests
{
    [Fact]
    public async Task AuthenticateDetailedAsync_IngresaConElNombreDeUsuario_SinDistinguirMayusculas()
    {
        var (unitOfWork, _) = BuildConOperario();
        var service = new AuthenticationService(unitOfWork);

        var result = await service.AuthenticateDetailedAsync("  OPERARIO ", "Clave.2026");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task AuthenticateDetailedAsync_RechazaElCorreoComoUsuario()
    {
        var (unitOfWork, usuario) = BuildConOperario();
        var service = new AuthenticationService(unitOfWork);

        var result = await service.AuthenticateDetailedAsync(usuario.Email, "Clave.2026");

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task CreateAsync_RechazaUnNombreYaUsado_SinDistinguirMayusculas()
    {
        var (unitOfWork, usuario) = BuildConOperario();
        var service = BuildUserManagement(unitOfWork);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(
            new UpsertUsuarioRequest { Nombre = "operario ", Email = "otro@test.com", RolId = usuario.RolId },
            "Clave.2026", "admin"));

        Assert.Contains("nombre de usuario", ex.Message);
        Assert.Single(unitOfWork.UsuariosFake.Items);
    }

    [Fact]
    public async Task UpdateAsync_RechazaTomarElNombreDeOtroUsuario()
    {
        var (unitOfWork, usuario) = BuildConOperario();
        var otro = new Usuario("Deposito", "deposito@test.com", BCrypt.Net.BCrypt.HashPassword("Clave.2026"), usuario.RolId);
        unitOfWork.UsuariosFake.Seed(otro);
        var service = BuildUserManagement(unitOfWork);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateAsync(otro.Id,
            new UpsertUsuarioRequest { Nombre = "Operario", Email = otro.Email, RolId = otro.RolId }, "admin"));

        Assert.Equal("Deposito", otro.Nombre);
    }

    [Fact]
    public async Task UpdateAsync_PermiteConservarElNombrePropio()
    {
        var (unitOfWork, usuario) = BuildConOperario();
        var service = BuildUserManagement(unitOfWork);

        var actualizado = await service.UpdateAsync(usuario.Id,
            new UpsertUsuarioRequest { Nombre = "Operario", Email = "nuevo@test.com", RolId = usuario.RolId }, "admin");

        Assert.Equal("nuevo@test.com", actualizado.Email);
    }

    [Fact]
    public async Task BaseDeDatos_ImpideNombresRepetidosConDistintasMayusculas()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<StockManufacturaDbContext>().UseSqlite(connection).Options;
        using var context = new StockManufacturaDbContext(options);
        context.Database.Migrate();
        var rol = new Rol("Operador", "Operación diaria");
        context.Roles.Add(rol);
        context.Usuarios.Add(new Usuario("Operario", "a@test.com", "hash", rol.Id));
        await context.SaveChangesAsync();

        context.Usuarios.Add(new Usuario("OPERARIO", "b@test.com", "hash", rol.Id));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Migracion_RenombraLosDuplicadosExistentesSinEliminarUsuarios()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<StockManufacturaDbContext>().UseSqlite(connection).Options;
        using (var context = new StockManufacturaDbContext(options))
        {
            var migrator = context.GetService<IMigrator>();
            var migraciones = context.Database.GetMigrations().ToList();
            var indice = migraciones.FindIndex(m => m.EndsWith("_UsuarioNombreUnico", StringComparison.Ordinal));
            migrator.Migrate(migraciones[indice - 1]);

            var rol = new Rol("Administrador", "Acceso completo");
            context.Roles.Add(rol);
            context.Usuarios.Add(new Usuario("Admin", "original@test.com", "hash", rol.Id));
            await context.SaveChangesAsync();
            await Task.Delay(20);
            context.Usuarios.Add(new Usuario("admin", "duplicado@test.com", "hash", rol.Id));
            await context.SaveChangesAsync();

            migrator.Migrate(migraciones[indice]);
        }

        using var verificacion = new StockManufacturaDbContext(options);
        var usuarios = await verificacion.Usuarios.OrderBy(u => u.CreatedAt).ToListAsync();
        Assert.Equal(2, usuarios.Count);
        Assert.Equal("Admin", usuarios[0].Nombre);
        Assert.Equal("admin (duplicado@test.com)", usuarios[1].Nombre);
    }

    private static (FakeUnitOfWork UnitOfWork, Usuario Usuario) BuildConOperario()
    {
        var rol = new Rol("Operador", "Operación diaria");
        var usuario = new Usuario("Operario", "operario@test.com", BCrypt.Net.BCrypt.HashPassword("Clave.2026"), rol.Id);
        usuario.AsignarRol(rol);
        var unitOfWork = new FakeUnitOfWork();
        unitOfWork.RolesFake.Seed(rol);
        unitOfWork.UsuariosFake.Seed(usuario);
        return (unitOfWork, usuario);
    }

    private static UserManagementService BuildUserManagement(FakeUnitOfWork unitOfWork)
    {
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<StockManufacturaMappingProfile>(), NullLoggerFactory.Instance).CreateMapper();
        return new UserManagementService(unitOfWork, mapper, new AuditLogService(unitOfWork));
    }
}
