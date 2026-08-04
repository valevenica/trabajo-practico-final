using System.Collections.Generic;
using System.Linq;

namespace StockManufactura.Domain.Entities
{
    public sealed class Rol : BaseEntity
    {
        private readonly List<string> _permisos = new();

        public string Nombre { get; private set; } = string.Empty;
        public string Descripcion { get; private set; } = string.Empty;
        public IReadOnlyCollection<string> Permisos => _permisos.AsReadOnly();

        private Rol() { }

        public Rol(string nombre, string descripcion)
        {
            Nombre = nombre;
            Descripcion = descripcion;
        }

        public void ActualizarDescripcion(string descripcion)
        {
            Descripcion = descripcion;
            UpdateTimestamp();
        }

        public void AsignarPermisos(IEnumerable<string> permisos)
        {
            _permisos.Clear();
            foreach (var permiso in permisos.Where(p => !string.IsNullOrWhiteSpace(p)))
            {
                _permisos.Add(permiso.Trim());
            }

            UpdateTimestamp();
        }

        public void AgregarPermisos(IEnumerable<string> permisos)
        {
            foreach (var permiso in permisos.Where(p => !string.IsNullOrWhiteSpace(p)))
            {
                var permisoNormalizado = permiso.Trim();
                if (!_permisos.Contains(permisoNormalizado))
                {
                    _permisos.Add(permisoNormalizado);
                }
            }

            UpdateTimestamp();
        }

        public void QuitarPermisos(IEnumerable<string> permisos)
        {
            foreach (var permiso in permisos.Where(p => !string.IsNullOrWhiteSpace(p)))
            {
                _permisos.RemoveAll(p => p.Equals(permiso.Trim(), System.StringComparison.OrdinalIgnoreCase));
            }

            UpdateTimestamp();
        }

        public bool TienePermiso(string permiso)
        {
            return _permisos.Contains(permiso);
        }
    }
}
