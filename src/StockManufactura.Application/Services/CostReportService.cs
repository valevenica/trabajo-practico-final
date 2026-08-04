using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StockManufactura.Application.Interfaces;
using StockManufactura.Application.Reports;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Application.Services
{
    public sealed class CostReportService : ICostReportService
    {
        private readonly IResourcePriceHistoryRepository _resourceHistoryRepository;
        private readonly IProductCostHistoryRepository _productCostHistoryRepository;

        public CostReportService(IResourcePriceHistoryRepository resourceHistoryRepository, IProductCostHistoryRepository productCostHistoryRepository)
        {
            _resourceHistoryRepository = resourceHistoryRepository;
            _productCostHistoryRepository = productCostHistoryRepository;
        }

        public async Task<IReadOnlyList<ResourcePriceHistory>> GetResourcePriceHistoryReportAsync(DateTime? from, DateTime? to, CancellationToken cancellationToken = default)
        {
            var all = await _resourceHistoryRepository.ListAsync();
            return all.Where(x => (!from.HasValue || x.Fecha >= from.Value) && (!to.HasValue || x.Fecha <= to.Value))
                .OrderByDescending(x => x.Fecha)
                .ToArray();
        }

        public async Task<IReadOnlyList<ProductCostHistory>> GetProductCostHistoryReportAsync(Guid? productId, DateTime? from, DateTime? to, CancellationToken cancellationToken = default)
        {
            var all = await _productCostHistoryRepository.ListAsync();
            return all.Where(x => (!productId.HasValue || x.ProductId == productId.Value) && (!from.HasValue || x.Fecha >= from.Value) && (!to.HasValue || x.Fecha <= to.Value))
                .OrderByDescending(x => x.Fecha)
                .ToArray();
        }

        public async Task<IReadOnlyList<CostImpactReportItem>> GetCostVariationReportAsync(DateTime? from, DateTime? to, CancellationToken cancellationToken = default)
        {
            var history = await GetProductCostHistoryReportAsync(null, from, to, cancellationToken);
            return history.Select(x => new CostImpactReportItem
            {
                Producto = x.Product?.Nombre ?? x.ProductId.ToString(),
                CostoAnterior = x.CostoAnterior,
                CostoNuevo = x.CostoNuevo,
                VariacionAbsoluta = x.CostoNuevo - x.CostoAnterior,
                VariacionPorcentual = x.CostoAnterior == 0 ? 0 : (x.CostoNuevo - x.CostoAnterior) / x.CostoAnterior
            }).ToArray();
        }

        public async Task<IReadOnlyList<CostImpactReportItem>> GetDollarImpactReportAsync(DateTime? from, DateTime? to, CancellationToken cancellationToken = default)
        {
            var history = await GetProductCostHistoryReportAsync(null, from, to, cancellationToken);
            return history.Where(x => x.CotizacionUtilizada > 1)
                .Select(x => new CostImpactReportItem
                {
                    Producto = x.Product?.Nombre ?? x.ProductId.ToString(),
                    CostoAnterior = x.CostoAnterior,
                    CostoNuevo = x.CostoNuevo,
                    VariacionAbsoluta = x.CostoNuevo - x.CostoAnterior,
                    VariacionPorcentual = x.CostoAnterior == 0 ? 0 : (x.CostoNuevo - x.CostoAnterior) / x.CostoAnterior
                }).ToArray();
        }

        public async Task<IReadOnlyList<CostImpactReportItem>> GetTopIncreasedProductsAsync(int top, CancellationToken cancellationToken = default)
        {
            var variation = await GetCostVariationReportAsync(null, null, cancellationToken);
            return variation.OrderByDescending(x => x.VariacionAbsoluta).Take(top).ToArray();
        }
    }
}
