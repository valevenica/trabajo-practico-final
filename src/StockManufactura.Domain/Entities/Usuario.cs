using System;

namespace StockManufactura.Domain.Entities
{
    public sealed class Usuario : BaseEntity
    {
        public string Nombre { get; private set; } = string.Empty;
        public string Email { get; private set; } = string.Empty;
        public string PasswordHash { get; private set; } = string.Empty;
        public Guid RolId { get; private set; }
        public Rol Rol { get; private set; } = null!;
        public bool EsActivo { get; private set; } = true;

        private Usuario() { }

        public Usuario(string nombre, string email, string passwordHash, Guid rolId)
        {
            Nombre = nombre;
            Email = email;
            PasswordHash = passwordHash;
            RolId = rolId;
        }

        public void Desactivar()
        {
            EsActivo = false;
            UpdateTimestamp();
        }

        public void Activar()
        {
            EsActivo = true;
            UpdateTimestamp();
        }

        public void CambiarPasswordHash(string passwordHash)
        {
            PasswordHash = passwordHash;
            UpdateTimestamp();
        }

        public void AsignarRol(Rol rol)
        {
            Rol = rol ?? throw new ArgumentNullException(nameof(rol));
            RolId = rol.Id;
            UpdateTimestamp();
        }
    }
}
