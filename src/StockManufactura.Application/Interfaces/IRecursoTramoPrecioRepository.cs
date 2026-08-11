using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Application.Interfaces
{
    public interface IRecursoTramoPrecioRepository : IRepository<RecursoTramoPrecio>
    {
        Task<IReadOnlyList<RecursoTramoPrecio>> ListByRecursoIdAsync(Guid recursoId);
        Task DeleteByRecursoIdAsync(Guid recursoId);
    }
}
