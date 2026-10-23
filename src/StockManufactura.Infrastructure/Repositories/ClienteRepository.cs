using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StockManufactura.Application.Interfaces;
using StockManufactura.Domain.Entities;
using StockManufactura.Infrastructure.Db;

namespace StockManufactura.Infrastructure.Repositories
{
    public sealed class ClienteRepository : Repository<Cliente>, IClienteRepository
    {
        private readonly StockManufacturaDbContext _context;

        public ClienteRepository(StockManufacturaDbContext context) : base(context)
        {
            _context = context;
        }

        public Task<Cliente?> GetByDocumentoAsync(string documento)
        {
            return _context.Clientes.FirstOrDefaultAsync(x => x.Documento == documento);
        }
    }
}
