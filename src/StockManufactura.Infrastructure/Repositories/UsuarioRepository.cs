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

            return await query.FirstOrDefaultAsync(x => x.Email == email);
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
