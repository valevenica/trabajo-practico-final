using System.Threading.Tasks;
using StockManufactura.Application.Interfaces;
using StockManufactura.Infrastructure.Db;

namespace StockManufactura.Infrastructure.Repositories
{
    public sealed class UnitOfWork : IUnitOfWork
    {
        private readonly StockManufacturaDbContext _context;

        public UnitOfWork(
            StockManufacturaDbContext context,
            IRolRepository rolRepository,
            IUsuarioRepository usuarioRepository)
        {
            _context = context;
            Roles = rolRepository;
            Usuarios = usuarioRepository;
        }

        public IRolRepository Roles { get; }
        public IUsuarioRepository Usuarios { get; }

        public Task<int> SaveChangesAsync() => _context.SaveChangesAsync();
    }
}
