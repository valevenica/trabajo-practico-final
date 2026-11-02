using Microsoft.EntityFrameworkCore;
using StockManufactura.Domain.Entities;
using StockManufactura.Infrastructure.Db;

namespace StockManufactura.Web.Infrastructure
{
    /// <summary>
    /// Modo demostración (configuración «Demo:Habilitada»), pensado para publicar la web en un hosting gratuito y que un
    /// evaluador la pruebe con usuarios conocidos. En cada arranque deja los usuarios de la demo activos, desbloqueados y con
    /// su contraseña de siempre, para que nadie quede afuera aunque se haya cambiado una contraseña o bloqueado una cuenta.
    /// </summary>
    public static class DemoSetup
    {
        public sealed record UsuarioDemo(string Nombre, string Password, string Email, string Rol, string Descripcion);

        private static readonly (string Nombre, string Descripcion, string[] Permisos)[] Roles =
        {
            ("Producción", "Rol de demostración: productos, insumos, órdenes e ingresos de stock, sin costos ni administración.",
                new[] { "PRODUCTOS_VER", "PRODUCTOS_CREAR", "PRODUCTOS_EDITAR", "STOCK_VER", "STOCK_AJUSTAR" }),
            ("Consulta", "Rol de demostración: solo consulta de productos, faltantes, costos e indicadores.",
                new[] { "PRODUCTOS_VER", "STOCK_VER", "COSTOS_VER" }),
        };

        public static readonly UsuarioDemo[] Usuarios =
        {
            new("profesor", "Profesor2026", "profesor@demo.local", DatabaseSeeder.AdminRoleName, "Administrador: acceso completo"),
            new("produccion", "Produccion2026", "produccion@demo.local", "Producción", "Productos, insumos, órdenes y stock"),
            new("consulta", "Consulta2026", "consulta@demo.local", "Consulta", "Solo consulta e indicadores"),
        };

        public static bool Habilitada(IConfiguration configuration) => configuration.GetValue<bool>("Demo:Habilitada");

        public static void Preparar(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<StockManufacturaDbContext>();

            foreach (var (nombre, descripcion, permisos) in Roles)
            {
                var rol = context.Roles.FirstOrDefault(r => r.Nombre == nombre);
                if (rol is null)
                {
                    rol = new Rol(nombre, descripcion);
                    context.Roles.Add(rol);
                }

                rol.AsignarPermisos(permisos);
            }

            context.SaveChanges();

            foreach (var demo in Usuarios)
            {
                var rol = context.Roles.First(r => r.Nombre == demo.Rol);
                var hash = BCrypt.Net.BCrypt.HashPassword(demo.Password);
                var usuario = context.Usuarios.AsEnumerable()
                    .FirstOrDefault(u => string.Equals(u.Nombre, demo.Nombre, StringComparison.OrdinalIgnoreCase));
                if (usuario is null)
                {
                    usuario = new Usuario(demo.Nombre, demo.Email, hash, rol.Id);
                    context.Usuarios.Add(usuario);
                }
                else
                {
                    usuario.ActualizarDatos(demo.Nombre, demo.Email);
                    usuario.CambiarPasswordHash(hash);
                    usuario.AsignarRol(rol);
                    usuario.Activar();
                }
            }

            context.SaveChanges();
        }
    }
}
