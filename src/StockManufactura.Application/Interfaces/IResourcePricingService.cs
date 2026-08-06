using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using StockManufactura.Application.Resources;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Application.Interfaces
{
    public interface IResourcePricingService
    {
        Task<IReadOnlyList<Recurso>> GetResourcesAsync(CancellationToken cancellationToken = default);
        Task<Recurso> UpsertResourceAsync(ResourceUpsertRequest request, CancellationToken cancellationToken = default);
        Task<ResourceCostQuote> CalculateCostAsync(Guid recursoId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ResourcePriceHistory>> GetPriceHistoryAsync(Guid recursoId, CancellationToken cancellationToken = default);
        Task<int> RecalcularTodosUSDAsync(string usuario, CancellationToken cancellationToken = default);
    }
}
