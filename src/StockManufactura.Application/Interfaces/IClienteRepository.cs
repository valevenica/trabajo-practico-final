using System.Threading.Tasks;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Application.Interfaces
{
    public interface IClienteRepository : IRepository<Cliente>
    {
        Task<Cliente?> GetByDocumentoAsync(string documento);
    }
}
