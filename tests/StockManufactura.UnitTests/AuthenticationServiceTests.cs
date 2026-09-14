#nullable enable

using System.Threading.Tasks;
using StockManufactura.Application.Services;
using StockManufactura.Domain.Entities;
using StockManufactura.UnitTests.TestDoubles;
using Xunit;

namespace StockManufactura.UnitTests;

public class AuthenticationServiceTests
{
    [Fact]
    public async Task AuthenticateAsync_ReturnsUser_WhenCredentialsAreValid()
    {
        var (unitOfWork, usuario) = BuildUnitOfWorkWithAdmin();
        var service = new AuthenticationService(unitOfWork);

        var result = await service.AuthenticateAsync("Admin", "Secret123!");

        Assert.NotNull(result);
        Assert.Equal(usuario.Email, result!.Email);
    }

    [Fact]
    public async Task RegisterLoginAsync_UpdatesLastAccess()
    {
        var (unitOfWork, usuario) = BuildUnitOfWorkWithAdmin();
        var service = new AuthenticationService(unitOfWork);

        var result = await service.RegisterLoginAsync(usuario);

        Assert.NotNull(result.UltimoAcceso);
        Assert.True(unitOfWork.SaveCalled);
    }

    [Fact]
    public async Task AuthenticateDetailedAsync_BlocksUserAfterMaxFailedAttempts()
    {
        var (unitOfWork, _) = BuildUnitOfWorkWithAdmin();
        var service = new AuthenticationService(unitOfWork);

        for (var i = 0; i < 5; i++)
        {
            await service.AuthenticateDetailedAsync("admin@test.com", "incorrecta");
        }

        var lockedResult = await service.AuthenticateDetailedAsync("admin@test.com", "Secret123!");

        Assert.False(lockedResult.IsSuccess);
        Assert.True(lockedResult.IsLockedOut);
    }

    private static (FakeUnitOfWork UnitOfWork, Usuario Usuario) BuildUnitOfWorkWithAdmin()
    {
        var rol = new Rol("Administrador", "Rol administrador");
        var usuario = new Usuario("Admin", "admin@test.com", BCrypt.Net.BCrypt.HashPassword("Secret123!"), rol.Id);
        usuario.AsignarRol(rol);

        var unitOfWork = new FakeUnitOfWork();
        unitOfWork.RolesFake.Seed(rol);
        unitOfWork.UsuariosFake.Seed(usuario);
        return (unitOfWork, usuario);
    }
}
