using StockManufactura.Web.Infrastructure;
using Xunit;

namespace StockManufactura.Web.Tests;

public class ModuleAccessTests
{
    public static IEnumerable<object[]> ModulosProtegidos() => new[]
    {
        ModuleAccess.BusinessIntelligence, ModuleAccess.Shortages, ModuleAccess.Products, ModuleAccess.Resources,
        ModuleAccess.Providers, ModuleAccess.Clients, ModuleAccess.ProductionOrders, ModuleAccess.StockIntake,
        ModuleAccess.SpecialResources, ModuleAccess.PriceImport, ModuleAccess.Monetary, ModuleAccess.CostHistory,
        ModuleAccess.Audit, ModuleAccess.Backups, ModuleAccess.Users,
    }.Select(x => new object[] { x });

    [Theory]
    [MemberData(nameof(ModulosProtegidos))]
    public void CadaModuloDelMenu_TienePermisosYNombreLegible(string modulo)
    {
        var permisos = ModuleAccess.Permisos(modulo);

        Assert.NotNull(permisos);
        Assert.NotEmpty(permisos!);
        Assert.NotEqual("este módulo", ModuleAccess.Nombre(modulo));
    }

    [Fact]
    public void PanelPrincipal_EsDeAccesoLibre()
    {
        Assert.Null(ModuleAccess.Permisos(ModuleAccess.Dashboard));
    }

    [Theory]
    [InlineData(ModuleAccess.Audit)]
    [InlineData(ModuleAccess.Backups)]
    [InlineData(ModuleAccess.Users)]
    public void ModulosDeAdministracion_SoloConUsuariosAdmin(string modulo)
    {
        Assert.Equal(new[] { "USUARIOS_ADMIN" }, ModuleAccess.Permisos(modulo));
    }

    [Fact]
    public void ModuloDesconocido_UsaNombreGenerico()
    {
        Assert.Null(ModuleAccess.Permisos("NoExiste"));
        Assert.Equal("este módulo", ModuleAccess.Nombre("NoExiste"));
    }
}
