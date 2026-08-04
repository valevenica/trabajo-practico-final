using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using StockManufactura.Application.Products;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Application.Interfaces
{
    public interface IProductCostService
    {
        Task RecalculateAffectedProductsAsync(ProductRecalculationRequest request, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ProductCostHistory>> GetProductCostHistoryAsync(Guid productId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ProductCostSnapshot>> GetProductSnapshotsAsync(Guid productId, CancellationToken cancellationToken = default);
        Task<ProductCostComparison> CompareVersionsAsync(Guid olderHistoryId, Guid newerHistoryId, CancellationToken cancellationToken = default);
    }
}
