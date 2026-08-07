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
        private readonly IUnitOfWork _unitOfWork;

        public ProductCostService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task RecalculateAffectedProductsAsync(ProductRecalculationRequest request, CancellationToken cancellationToken = default)
        {
            var allProducts = await _unitOfWork.Productos.ListActivosAsync();
            var allRecipeItems = await _unitOfWork.RecetaProductoItems.ListByProductIdsAsync(allProducts.Select(x => x.Id));
            var recipeByProduct = allRecipeItems
                .GroupBy(x => x.ProductoId)
                .ToDictionary(g => g.Key, g => (IReadOnlyList<RecetaProductoItem>)g.ToList());

            HashSet<Guid> impactedIds;
            if (request.RecursoDisparadorId.HasValue)
            {
                impactedIds = allRecipeItems
                    .Where(x => x.RecursoId == request.RecursoDisparadorId.Value)
                    .Select(x => x.ProductoId)
                    .ToHashSet();
            }
            else if (request.ProductoDisparadorId.HasValue)
            {
                impactedIds = new HashSet<Guid> { request.ProductoDisparadorId.Value };
            }
            else
            {
                impactedIds = allProducts.Select(x => x.Id).ToHashSet();
            }

            // A product that uses an impacted product as a sub-component is impacted too (transitively).
            var grew = true;
            while (grew)
            {
                grew = false;
                foreach (var (productId, items) in recipeByProduct)
                {
                    if (impactedIds.Contains(productId))
                    {
                        continue;
                    }

                    if (items.Any(item => item.ComponenteProductoId.HasValue && impactedIds.Contains(item.ComponenteProductoId.Value)))
                    {
                        impactedIds.Add(productId);
                        grew = true;
                    }
                }
            }

            var productsById = allProducts.ToDictionary(x => x.Id);
            var orderedIds = OrderByComponentDependency(impactedIds, recipeByProduct);

            var latestRate = await _unitOfWork.ExchangeRates.GetLatestAsync();
            var exchangeRateValue = latestRate?.Valor ?? 1m;

            foreach (var productId in orderedIds)
            {
                if (!productsById.TryGetValue(productId, out var product))
                {
                    continue;
                }

                var recipeItems = recipeByProduct.TryGetValue(product.Id, out var items)
                    ? items
                    : Array.Empty<RecetaProductoItem>();

                var previousCost = product.CostoFabricacionActual;
                var previousSuggested = product.PrecioSugeridoActual;

                var newCost = recipeItems.Sum(x => x.Cantidad * GetUnitCost(x));
                var newSuggested = newCost * (1 + product.MargenActual);

                product.CostoFabricacionActual = newCost;
                product.PrecioSugeridoActual = newSuggested;
                product.FechaUltimoCalculo = DateTime.UtcNow;
                _unitOfWork.Productos.Update(product);

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

                await _unitOfWork.ProductCostHistory.AddAsync(history);

                var snapshot = new ProductCostSnapshot
                {
                    ProductId = product.Id,
                    Fecha = DateTime.UtcNow,
                    CostoTotal = newCost,
                    CotizacionUtilizada = exchangeRateValue,
                    CostoFinal = newCost
                };

                await _unitOfWork.ProductCostSnapshots.AddAsync(snapshot);

                foreach (var item in recipeItems.Where(x => x.RecursoId.HasValue))
                {
                    var snapshotItem = new ProductCostSnapshotItem
                    {
                        SnapshotId = snapshot.Id,
                        RecursoId = item.RecursoId!.Value,
                        CantidadUtilizada = item.Cantidad,
                        PrecioRecurso = item.Recurso!.Precio,
                        CotizacionUtilizada = exchangeRateValue,
                        CostoParcial = item.Cantidad * item.Recurso.Precio
                    };

                    await _unitOfWork.ProductCostSnapshotItems.AddAsync(snapshotItem);
                }
            }

            await _unitOfWork.SaveChangesAsync();
        }

        private static decimal GetUnitCost(RecetaProductoItem item)
        {
            return item.RecursoId.HasValue ? item.Recurso!.Precio : item.ComponenteProducto!.CostoFabricacionActual;
        }

        private static List<Guid> OrderByComponentDependency(HashSet<Guid> impactedIds, Dictionary<Guid, IReadOnlyList<RecetaProductoItem>> recipeByProduct)
        {
            var ordered = new List<Guid>();
            var visited = new HashSet<Guid>();
            var visiting = new HashSet<Guid>();

            void Visit(Guid productId)
            {
                if (visited.Contains(productId) || !impactedIds.Contains(productId) || !visiting.Add(productId))
                {
                    return;
                }

                if (recipeByProduct.TryGetValue(productId, out var items))
                {
                    foreach (var item in items.Where(x => x.ComponenteProductoId.HasValue))
                    {
                        Visit(item.ComponenteProductoId!.Value);
                    }
                }

                visited.Add(productId);
                ordered.Add(productId);
            }

            foreach (var id in impactedIds)
            {
                Visit(id);
            }

            return ordered;
        }

        public Task<IReadOnlyList<ProductCostHistory>> GetProductCostHistoryAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            return _unitOfWork.ProductCostHistory.ListByProductAsync(productId);
        }

        public Task<IReadOnlyList<ProductCostSnapshot>> GetProductSnapshotsAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            return _unitOfWork.ProductCostSnapshots.ListByProductAsync(productId);
        }

        public async Task<IReadOnlyList<ProductCostSummary>> GetCostSummaryAsync(CancellationToken cancellationToken = default)
        {
            var products = await _unitOfWork.Productos.ListActivosAsync();
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
            var older = await _unitOfWork.ProductCostHistory.GetByIdAsync(olderHistoryId) ?? throw new InvalidOperationException("Version antigua no encontrada.");
            var newer = await _unitOfWork.ProductCostHistory.GetByIdAsync(newerHistoryId) ?? throw new InvalidOperationException("Version nueva no encontrada.");

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
