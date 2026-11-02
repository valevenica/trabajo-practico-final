using System.Net;
using Xunit;

namespace StockManufactura.Web.Tests;

/// <summary>
/// Pruebas de integración: cada petición recorre la canalización real de ASP.NET Core (autenticación por cookie,
/// autorización, antifalsificación, componentes de Blazor y el punto de descarga de copias).
/// </summary>
public class SitioWebTests : IClassFixture<SitioWebFactory>
{
    private readonly SitioWebFactory _sitio;

    public SitioWebTests(SitioWebFactory sitio) => _sitio = sitio;

    [Fact]
    public async Task Login_EsPublicoYOfreceRecuperarLaContrasena()
    {
        var respuesta = await _sitio.Cliente().GetAsync("/login");
        var html = await respuesta.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("¿Olvidó su contraseña?", html);
        Assert.Contains("btn-eye", html);
    }

    [Fact]
    public async Task RecuperarContrasena_EsPublica()
    {
        var respuesta = await _sitio.Cliente().GetAsync("/cuenta/recuperar");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/inteligencia")]
    [InlineData("/productos")]
    [InlineData("/insumos")]
    [InlineData("/ordenes")]
    [InlineData("/usuarios")]
    [InlineData("/backups")]
    [InlineData("/bitacora")]
    [InlineData("/cuenta")]
    public async Task PantallasDelSistema_SinSesionRedirigenAlIngreso(string ruta)
    {
        var respuesta = await _sitio.Cliente().GetAsync(ruta);

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Contains("/login", respuesta.Headers.Location!.OriginalString);
    }

    [Theory]
    [InlineData("/js/app.js")]
    [InlineData("/img/logo.png")]
    [InlineData("/app.css")]
    public async Task ArchivosEstaticos_SeSirven(string ruta)
    {
        var respuesta = await _sitio.Cliente().GetAsync(ruta);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    [Fact]
    public async Task PasswordIncorrecta_MuestraErrorYNoCreaSesion()
    {
        var cliente = _sitio.Cliente();

        var respuesta = await SitioWebFactory.IngresarAsync(cliente, "Admin", "incorrecta");
        var html = await respuesta.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("alert-error", html);
        Assert.Equal(HttpStatusCode.Redirect, (await cliente.GetAsync("/")).StatusCode);
    }

    [Fact]
    public async Task AdminInicial_DebeCambiarLaContrasenaAlIngresar()
    {
        var respuesta = await SitioWebFactory.IngresarAsync(_sitio.Cliente(), "Admin", SitioWebFactory.AdminPasswordInicial);

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.EndsWith("cuenta/password", respuesta.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task IngresoCorrecto_VuelveALaPaginaPedida()
    {
        await _sitio.CrearUsuarioAsync("compras", "Compras123", "PRODUCTOS_EDITAR");
        var cliente = _sitio.Cliente();

        var respuesta = await SitioWebFactory.IngresarAsync(cliente, "compras", "Compras123", "/login?ReturnUrl=%2Fproductos");

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.EndsWith("/productos", respuesta.Headers.Location!.OriginalString);
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/productos")).StatusCode);
    }

    [Theory]
    [InlineData("externo1", "https://sitio-externo.com/")]
    [InlineData("externo2", "//sitio-externo.com/")]
    public async Task IngresoCorrecto_NoRedirigeASitiosExternos(string usuario, string destino)
    {
        await _sitio.CrearUsuarioAsync(usuario, "Usuario123", "STOCK_VER");

        var respuesta = await SitioWebFactory.IngresarAsync(_sitio.Cliente(), usuario, "Usuario123", "/login?ReturnUrl=" + Uri.EscapeDataString(destino));

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.DoesNotContain("sitio-externo", respuesta.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task RolRestringido_SoloVeEnElMenuSusModulos()
    {
        await _sitio.CrearUsuarioAsync("consulta", "Consulta123", "STOCK_VER");
        var cliente = _sitio.Cliente();
        await SitioWebFactory.IngresarAsync(cliente, "consulta", "Consulta123");

        // El menú se arma en el servidor; el aviso de acceso denegado de cada pantalla lo cubre la prueba en navegador.
        var panel = await cliente.GetStringAsync("/");

        Assert.Contains("href=\"faltantes\"", panel);
        Assert.DoesNotContain("href=\"productos\"", panel);
        Assert.DoesNotContain("href=\"backups\"", panel);
        Assert.DoesNotContain("href=\"usuarios\"", panel);
        Assert.DoesNotContain("href=\"bitacora\"", panel);
    }

    [Fact]
    public async Task DescargaDeCopia_SinSesionPideIngresar()
    {
        var respuesta = await _sitio.Cliente().GetAsync($"/backups/{Guid.NewGuid()}/descargar");

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Contains("/login", respuesta.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task DescargaDeCopia_SinPermisoDeAdministracionDevuelve403()
    {
        await _sitio.CrearUsuarioAsync("deposito", "Deposito123", "STOCK_VER");
        var cliente = _sitio.Cliente();
        await SitioWebFactory.IngresarAsync(cliente, "deposito", "Deposito123");

        var respuesta = await cliente.GetAsync($"/backups/{Guid.NewGuid()}/descargar");

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    [Fact]
    public async Task DescargaDeCopia_InexistenteDevuelve404AlAdministrador()
    {
        await _sitio.CrearUsuarioAsync("sistemas", "Sistemas123", "USUARIOS_ADMIN");
        var cliente = _sitio.Cliente();
        await SitioWebFactory.IngresarAsync(cliente, "sistemas", "Sistemas123");

        var respuesta = await cliente.GetAsync($"/backups/{Guid.NewGuid()}/descargar");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    [Fact]
    public async Task CerrarSesion_BorraLaCookie()
    {
        await _sitio.CrearUsuarioAsync("ventas", "Ventas123", "PRODUCTOS_VER");
        var cliente = _sitio.Cliente();
        await SitioWebFactory.IngresarAsync(cliente, "ventas", "Ventas123");
        var pagina = await cliente.GetStringAsync("/");

        var salida = await cliente.PostAsync("/cuenta/salir", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = SitioWebFactory.CampoOculto(pagina, "__RequestVerificationToken"),
        }));

        Assert.Equal(HttpStatusCode.Redirect, salida.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await cliente.GetAsync("/")).StatusCode);
    }
}
