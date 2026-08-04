using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using StockManufactura.Application.Reports;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Application.Interfaces
{
    public interface ICostReportService
    {
        Task<IReadOnlyList<ResourcePriceHistory>> GetResourcePriceHistoryReportAsync(DateTime? from, DateTime? to, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ProductCostHistory>> GetProductCostHistoryReportAsync(Guid? productId, DateTime? from, DateTime? to, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<CostImpactReportItem>> GetCostVariationReportAsync(DateTime? from, DateTime? to, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<CostImpactReportItem>> GetDollarImpactReportAsync(DateTime? from, DateTime? to, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<CostImpactReportItem>> GetTopIncreasedProductsAsync(int top, CancellationToken cancellationToken = default);
    }
}
