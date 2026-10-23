using StockManufactura.Domain.Entities;

namespace StockManufactura.Desktop.Infrastructure
{
    public sealed class AuthSession
    {
        public AuthSession(Usuario usuario)
        {
            Usuario = usuario;
        }

        public static AuthSession? Current { get; set; }

        public Usuario Usuario { get; }

        public bool TienePermiso(string permiso)
        {
            // Solo el rol decide los permisos: el nombre o el correo del usuario no otorgan privilegios,
            // porque cualquier administrador puede crear un usuario llamado "Admin".
            var roleName = Usuario.Rol?.Nombre?.Trim();
            if (string.Equals(roleName, DatabaseSeeder.AdminRoleName, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return Usuario.Rol?.TienePermiso(permiso) == true;
        }
    }
}
