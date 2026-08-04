using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Application.Interfaces
{
    public interface IProductCostSnapshotItemRepository : IRepository<ProductCostSnapshotItem>
    {
        Task<IReadOnlyList<ProductCostSnapshotItem>> ListBySnapshotAsync(Guid snapshotId);
    }
}
