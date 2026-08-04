using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Application.Interfaces
{
    public interface IResourcePriceHistoryRepository : IRepository<ResourcePriceHistory>
    {
        Task<IReadOnlyList<ResourcePriceHistory>> ListByResourceAsync(Guid recursoId);
    }
}
