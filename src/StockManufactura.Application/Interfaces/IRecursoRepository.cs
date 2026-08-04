using System.Collections.Generic;
using System.Threading.Tasks;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Application.Interfaces
{
    public interface IRecursoRepository : IRepository<Recurso>
    {
        Task<Recurso?> GetByCodigoAsync(string codigo);
        Task<IReadOnlyList<Recurso>> ListActivosAsync();
    }
}
