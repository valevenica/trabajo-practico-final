using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Application.Interfaces
{
    public interface IRecetaProductoItemRepository : IRepository<RecetaProductoItem>
    {
        Task<IReadOnlyList<RecetaProductoItem>> ListByProductIdAsync(Guid productId);
        Task<IReadOnlyList<RecetaProductoItem>> ListByProductIdsAsync(IEnumerable<Guid> productIds);
        Task<IReadOnlyList<RecetaProductoItem>> ListByResourceIdAsync(Guid resourceId);
    }
}
