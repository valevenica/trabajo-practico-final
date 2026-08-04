using System.Collections.Generic;
using System.Threading.Tasks;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Application.Interfaces
{
    public interface IOrdenProduccionRepository : IRepository<OrdenProduccion>
    {
        Task<OrdenProduccion?> GetByCodigoAsync(string codigo);
        Task<IReadOnlyList<OrdenProduccion>> ListByCreatedDescAsync();
    }
}
