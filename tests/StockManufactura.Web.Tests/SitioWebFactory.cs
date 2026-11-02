using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using StockManufactura.Application.Interfaces;
using StockManufactura.Domain.Entities;
using StockManufactura.Web.Infrastructure;

namespace StockManufactura.Web.Tests;

/// <summary>
/// Levanta el sitio completo en memoria sobre una base SQLite temporal, sembrada igual que en producción (rol
/// Administrador y usuario Admin con cambio de contraseña obligatorio).
/// </summary>
public class SitioWebFactory : WebApplicationFactory<Program>
{
    public const string AdminPasswordInicial = "Admin123";

    private readonly string _baseDeDatos = Path.Combine(Path.GetTempPath(), $"stockmanufactura-web-tests-{Guid.NewGuid():N}.db");

    protected virtual bool Demo => false;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:StockManufactura", $"Data Source={_baseDeDatos};Pooling=False");
        builder.UseSetting("Demo:Habilitada", Demo.ToString());
        builder.ConfigureServices(services =>
        {
            // Las pruebas no deben generar copias de seguridad en la carpeta del usuario.
            var copias = services.Single(d => d.ImplementationType == typeof(AutomaticBackupService));
            services.Remove(copias);
        });
    }

    public HttpClient Cliente() => CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });

    /// <summary>Crea un usuario activo, sin cambio de contraseña pendiente, con un rol que tiene solo los permisos dados.</summary>
    public async Task CrearUsuarioAsync(string nombre, string password, params string[] permisos)
    {
        using var scope = Services.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var rol = new Rol($"Rol {nombre}", "Rol de prueba");
        rol.AsignarPermisos(permisos);
        await unitOfWork.Roles.AddAsync(rol);
        var usuario = new Usuario(nombre, $"{nombre}@test.com", BCrypt.Net.BCrypt.HashPassword(password), rol.Id);
        usuario.MarcarCambioPasswordRealizado();
        await unitOfWork.Usuarios.AddAsync(usuario);
        await unitOfWork.SaveChangesAsync();
    }

    /// <summary>Envía el formulario de ingreso igual que el navegador, con su token antifalsificación.</summary>
    public static async Task<HttpResponseMessage> IngresarAsync(HttpClient cliente, string usuario, string password, string ruta = "/login")
    {
        var pagina = await cliente.GetStringAsync(ruta);
        var formulario = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = CampoOculto(pagina, "__RequestVerificationToken"),
            ["_handler"] = CampoOculto(pagina, "_handler"),
            ["Input.Usuario"] = usuario,
            ["Input.Password"] = password,
        };
        return await cliente.PostAsync(ruta, new FormUrlEncodedContent(formulario));
    }

    /// <summary>Lee un campo oculto de un formulario renderizado (el orden de los atributos no está garantizado).</summary>
    public static string CampoOculto(string html, string nombre)
    {
        foreach (Match input in Regex.Matches(html, "<input[^>]*>"))
        {
            if (!input.Value.Contains($"name=\"{nombre}\""))
            {
                continue;
            }

            var valor = Regex.Match(input.Value, "value=\"([^\"]*)\"");
            if (valor.Success)
            {
                return WebUtility.HtmlDecode(valor.Groups[1].Value);
            }
        }

        throw new InvalidOperationException($"La página no tiene el campo {nombre}.");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        foreach (var archivo in new[] { _baseDeDatos, _baseDeDatos + "-wal", _baseDeDatos + "-shm" })
        {
            try { File.Delete(archivo); } catch (IOException) { }
        }
    }
}

/// <summary>El mismo sitio con el modo demostración activado, como se publica en el hosting.</summary>
public sealed class SitioDemoFactory : SitioWebFactory
{
    protected override bool Demo => true;
}
