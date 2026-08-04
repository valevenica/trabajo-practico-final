using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Application.Interfaces
{
    public interface IStockRepository : IRepository<Stock>
    {
        Task<Stock?> GetByProductoYUbicacionAsync(Guid productoId, Guid ubicacionId);
        Task<IReadOnlyList<Stock>> ListByProductoAsync(Guid productoId);
    }
}
