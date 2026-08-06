using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StockManufactura.Application.Interfaces;
using StockManufactura.Application.Products;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Application.Services
{
    public sealed class ProductCostService : IProductCostService
    {
        private readonly IProductoRepository _productoRepository;
        private readonly IRecetaProductoItemRepository _recetaRepository;
        private readonly IProductCostHistoryRepository _historyRepository;
        private readonly IProductCostSnapshotRepository _snapshotRepository;
        private readonly IProductCostSnapshotItemRepository _snapshotItemRepository;
        private readonly IExchangeRateRepository _exchangeRateRepository;
        private readonly IUnitOfWork _unitOfWork;

        public ProductCostService(
            IProductoRepository productoRepository,
            IRecetaProductoItemRepository recetaRepository,
            IProductCostHistoryRepository historyRepository,
            IProductCostSnapshotRepository snapshotRepository,
            IProductCostSnapshotItemRepository snapshotItemRepository,
            IExchangeRateRepository exchangeRateRepository,
            IUnitOfWork unitOfWork)
        {
            _productoRepository = productoRepository;
            _recetaRepository = recetaRepository;
            _historyRepository = historyRepository;
            _snapshotRepository = snapshotRepository;
            _snapshotItemRepository = snapshotItemRepository;
            _exchangeRateRepository = exchangeRateRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task RecalculateAffectedProductsAsync(ProductRecalculationRequest request, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Producto> impactedProducts;
            if (request.RecursoDisparadorId.HasValue)
            {
                var recipeItems = await _recetaRepository.ListByResourceIdAsync(request.RecursoDisparadorId.Value);
                var productIds = recipeItems.Select(x => x.ProductoId).Distinct().ToArray();
                var allProducts = await _productoRepository.ListActivosAsync();
                impactedProducts = allProducts.Where(x => productIds.Contains(x.Id)).ToArray();
            }
            else
            {
                impactedProducts = await _productoRepository.ListActivosAsync();
            }

            var latestRate = await _exchangeRateRepository.GetLatestAsync();
            var exchangeRateValue = latestRate?.Valor ?? 1m;

            // Batch-load all recipe items for all impacted products in one query
            var allRecipeItems = await _recetaRepository.ListByProductIdsAsync(impactedProducts.Select(x => x.Id));
            var recipeByProduct = allRecipeItems
                .GroupBy(x => x.ProductoId)
                .ToDictionary(g => g.Key, g => (IReadOnlyList<RecetaProductoItem>)g.ToList());

            foreach (var product in impactedProducts)
            {
                var recipeItems = recipeByProduct.TryGetValue(product.Id, out var items)
                    ? items
                    : Array.Empty<RecetaProductoItem>();

                var previousCost = product.CostoFabricacionActual;
                var previousSuggested = product.PrecioSugeridoActual;

                var total = recipeItems.Sum(x => x.Cantidad * x.Recurso.Precio);
                var newCost = total;
                var newSuggested = newCost * (1 + product.MargenActual);

                product.CostoFabricacionActual = newCost;
                product.PrecioSugeridoActual = newSuggested;
                product.FechaUltimoCalculo = DateTime.UtcNow;

                var history = new ProductCostHistory
                {
                    ProductId = product.Id,
                    Fecha = DateTime.UtcNow,
                    Usuario = request.Usuario,
                    CostoAnterior = previousCost,
                    CostoNuevo = newCost,
                    PrecioSugeridoAnterior = previousSuggested,
                    PrecioSugeridoNuevo = newSuggested,
                    MargenUtilizado = product.MargenActual,
                    CotizacionUtilizada = exchangeRateValue,
                    MotivoRecalculo = request.Motivo,
                    Observaciones = "Recalculo automatico"
                };

                await _historyRepository.AddAsync(history);

                var snapshot = new ProductCostSnapshot
                {
                    ProductId = product.Id,
                    Fecha = DateTime.UtcNow,
                    CostoTotal = newCost,
                    CotizacionUtilizada = exchangeRateValue,
                    CostoFinal = newCost
                };

                await _snapshotRepository.AddAsync(snapshot);

                foreach (var item in recipeItems)
                {
                    var snapshotItem = new ProductCostSnapshotItem
                    {
                        SnapshotId = snapshot.Id,
                        RecursoId = item.RecursoId,
                        CantidadUtilizada = item.Cantidad,
                        PrecioRecurso = item.Recurso.Precio,
                        CotizacionUtilizada = exchangeRateValue,
                        CostoParcial = item.Cantidad * item.Recurso.Precio
                    };

                    await _snapshotItemRepository.AddAsync(snapshotItem);
                }
            }

            await _unitOfWork.SaveChangesAsync();
        }

        public Task<IReadOnlyList<ProductCostHistory>> GetProductCostHistoryAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            return _historyRepository.ListByProductAsync(productId);
        }

        public Task<IReadOnlyList<ProductCostSnapshot>> GetProductSnapshotsAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            return _snapshotRepository.ListByProductAsync(productId);
        }

        public async Task<IReadOnlyList<ProductCostSummary>> GetCostSummaryAsync(CancellationToken cancellationToken = default)
        {
            var products = await _productoRepository.ListActivosAsync();
            return products
                .Select(product => new ProductCostSummary
                {
                    ProductId = product.Id,
                    ProductName = product.Nombre,
                    CosteActual = product.CostoFabricacionActual,
                    PrecioSugerido = product.PrecioSugeridoActual,
                    Margen = product.MargenActual
                })
                .ToArray();
        }

        public async Task<ProductCostComparison> CompareVersionsAsync(Guid olderHistoryId, Guid newerHistoryId, CancellationToken cancellationToken = default)
        {
            var older = await _historyRepository.GetByIdAsync(olderHistoryId) ?? throw new InvalidOperationException("Version antigua no encontrada.");
            var newer = await _historyRepository.GetByIdAsync(newerHistoryId) ?? throw new InvalidOperationException("Version nueva no encontrada.");

            var absolute = newer.CostoNuevo - older.CostoNuevo;
            var percentage = older.CostoNuevo == 0 ? 0 : absolute / older.CostoNuevo;

            return new ProductCostComparison
            {
                ProductId = newer.ProductId,
                FechaAnterior = older.Fecha,
                FechaNueva = newer.Fecha,
                CostoAnterior = older.CostoNuevo,
                CostoNuevo = newer.CostoNuevo,
                VariacionAbsoluta = absolute,
                VariacionPorcentual = percentage,
                CotizacionUtilizada = newer.CotizacionUtilizada,
                RecursosModificados = Array.Empty<string>()
            };
        }
    }
}
