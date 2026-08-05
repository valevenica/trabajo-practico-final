using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Application.Interfaces
{
    public interface IRecursoProveedorRepository : IRepository<RecursoProveedor>
    {
        Task<IReadOnlyList<RecursoProveedor>> ListByRecursoIdAsync(Guid recursoId);
        Task<RecursoProveedor?> GetPrioritarioAsync(Guid recursoId);
    }
}
