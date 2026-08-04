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
        public DateTime? UltimoAcceso { get; private set; }
        public bool RequiereCambioPassword { get; private set; }
        public int IntentosFallidosLogin { get; private set; }
        public DateTime? BloqueadoHastaUtc { get; private set; }

        private Usuario() { }

        public Usuario(string nombre, string email, string passwordHash, Guid rolId)
        {
            Nombre = nombre;
            Email = email;
            PasswordHash = passwordHash;
            RolId = rolId;
        }

        public void ActualizarDatos(string nombre, string email)
        {
            Nombre = nombre;
            Email = email;
            UpdateTimestamp();
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
            RequiereCambioPassword = false;
            IntentosFallidosLogin = 0;
            BloqueadoHastaUtc = null;
            UpdateTimestamp();
        }

        public void ForzarCambioPassword()
        {
            RequiereCambioPassword = true;
            UpdateTimestamp();
        }

        public bool EstaBloqueado(DateTime utcNow)
        {
            return BloqueadoHastaUtc.HasValue && BloqueadoHastaUtc.Value > utcNow;
        }

        public int RegistrarIntentoFallido(DateTime utcNow, int maxIntentos, TimeSpan duracionBloqueo)
        {
            IntentosFallidosLogin++;
            if (IntentosFallidosLogin >= maxIntentos)
            {
                BloqueadoHastaUtc = utcNow.Add(duracionBloqueo);
                IntentosFallidosLogin = 0;
            }

            UpdateTimestamp();
            return IntentosFallidosLogin;
        }

        public void RegistrarAcceso()
        {
            UltimoAcceso = DateTime.UtcNow;
            IntentosFallidosLogin = 0;
            BloqueadoHastaUtc = null;
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
