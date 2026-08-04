using System.Collections.Generic;
using System.Threading.Tasks;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Application.Interfaces
{
    public interface IProductoRepository : IRepository<Producto>
    {
        Task<Producto?> GetByCodigoAsync(string codigo);
        Task<IReadOnlyList<Producto>> ListActivosAsync();
    }
}
