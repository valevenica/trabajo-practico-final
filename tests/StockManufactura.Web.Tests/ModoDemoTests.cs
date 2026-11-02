using System.Net;
using StockManufactura.Web.Infrastructure;
using Xunit;

namespace StockManufactura.Web.Tests;

/// <summary>Modo demostración: los usuarios publicados en la pantalla de ingreso existen y pueden entrar.</summary>
public class ModoDemoTests : IClassFixture<SitioDemoFactory>
{
    private readonly SitioDemoFactory _sitio;

    public ModoDemoTests(SitioDemoFactory sitio) => _sitio = sitio;

    [Fact]
    public async Task Ingreso_MuestraLosUsuariosDeLaDemo()
    {
        var html = await _sitio.Cliente().GetStringAsync("/login");

        Assert.Contains("demo-box", html);
        Assert.All(DemoSetup.Usuarios, u => Assert.Contains(u.Nombre, html));
    }

    [Theory]
    [InlineData("profesor", "Profesor2026")]
    [InlineData("produccion", "Produccion2026")]
    [InlineData("consulta", "Consulta2026")]
    public async Task UsuariosDeLaDemo_IngresanSinCambioDeContrasena(string usuario, string password)
    {
        var respuesta = await SitioWebFactory.IngresarAsync(_sitio.Cliente(), usuario, password);

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.DoesNotContain("cuenta/password", respuesta.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task SitioNormal_NoMuestraElRecuadroDeDemo()
    {
        using var normal = new SitioWebFactory();

        var html = await normal.Cliente().GetStringAsync("/login");

        Assert.DoesNotContain("demo-box", html);
    }
}
