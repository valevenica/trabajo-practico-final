using StockManufactura.Domain.Entities;
using Xunit;

namespace StockManufactura.UnitTests;

public class AuthenticationTests
{
    [Fact]
    public void RolConPermisos_ExponePermisoCorrecto()
    {
        var rol = new Rol("Producción", "Rol de producción");
        rol.AsignarPermisos(new[] { "PRODUCTOS_VER", "STOCK_VER" });

        Assert.True(rol.TienePermiso("PRODUCTOS_VER"));
        Assert.False(rol.TienePermiso("USUARIOS_ADMIN"));
    }

    [Fact]
    public void RolPuedeAgregarYQuitarPermisosSinDuplicados()
    {
        var rol = new Rol("Administración", "Rol administrativo");
        rol.AsignarPermisos(new[] { "PRODUCTOS_VER", "STOCK_VER" });

        rol.AgregarPermisos(new[] { "USUARIOS_ADMIN", "PRODUCTOS_VER" });
        Assert.True(rol.TienePermiso("USUARIOS_ADMIN"));
        Assert.True(rol.TienePermiso("PRODUCTOS_VER"));

        rol.QuitarPermisos(new[] { "PRODUCTOS_VER" });
        Assert.False(rol.TienePermiso("PRODUCTOS_VER"));
        Assert.True(rol.TienePermiso("STOCK_VER"));
    }
}
