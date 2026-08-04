using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Application.Interfaces
{
    public interface IProductCostHistoryRepository : IRepository<ProductCostHistory>
    {
        Task<IReadOnlyList<ProductCostHistory>> ListByProductAsync(Guid productId);
    }
}
