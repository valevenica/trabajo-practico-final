using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Application.Interfaces
{
    public interface IProductCostSnapshotRepository : IRepository<ProductCostSnapshot>
    {
        Task<IReadOnlyList<ProductCostSnapshot>> ListByProductAsync(Guid productId);
    }
}
