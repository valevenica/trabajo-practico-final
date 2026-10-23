using StockManufactura.Application.Interfaces;
using StockManufactura.Domain.Entities;
using StockManufactura.Infrastructure.Db;
using Microsoft.EntityFrameworkCore;

namespace StockManufactura.Infrastructure.Repositories
{
    public sealed class UsuarioRepository : Repository<Usuario>, IUsuarioRepository
    {
        private readonly StockManufacturaDbContext _context;

        public UsuarioRepository(StockManufacturaDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Usuario?> GetByEmailAsync(string email, bool includeRole = false)
        {
            var query = _context.Usuarios.AsQueryable();
            if (includeRole)
            {
                query = query.Include(x => x.Rol);
            }

            return await RefreshAsync(await query.FirstOrDefaultAsync(x => x.Email == email));
        }

        public async Task<Usuario?> GetByNombreAsync(string nombre, bool includeRole = false)
        {
            var normalized = (nombre ?? string.Empty).Trim().ToLower();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return null;
            }

            var query = _context.Usuarios.AsQueryable();
            if (includeRole)
            {
                query = query.Include(x => x.Rol);
            }

            return await RefreshAsync(await query.FirstOrDefaultAsync(x => x.Nombre.ToLower() == normalized));
        }

        /// <summary>
        /// La pantalla de ingreso conserva su contexto mientras está abierta. Si el usuario ya estaba en seguimiento,
        /// la consulta devuelve la copia en memoria y no el estado persistido, de modo que un desbloqueo o un cambio de
        /// contraseña hecho desde otro contexto (recuperación por correo, administrador) no se vería hasta reiniciar.
        /// Se recarga solo si no tiene cambios pendientes, para no descartar una edición en curso.
        /// </summary>
        private async Task<Usuario?> RefreshAsync(Usuario? usuario)
        {
            if (usuario is not null && _context.Entry(usuario).State == EntityState.Unchanged)
            {
                await _context.Entry(usuario).ReloadAsync();
            }

            return usuario;
        }

        public Task<Usuario?> GetByIdWithRoleAsync(Guid id)
        {
            return _context.Usuarios
                .Include(x => x.Rol)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<IReadOnlyList<Usuario>> ListWithRoleAsync()
        {
            return await _context.Usuarios
                .AsNoTracking()
                .Include(x => x.Rol)
                .OrderBy(x => x.Nombre)
                .ToListAsync();
        }
    }
}
