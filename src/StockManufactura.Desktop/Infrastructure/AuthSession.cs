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
            return Usuario.Rol?.TienePermiso(permiso) == true;
        }
    }
}
