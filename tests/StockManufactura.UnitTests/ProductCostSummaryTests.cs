using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StockManufactura.Application.Interfaces;
using StockManufactura.Application.Products;
using StockManufactura.Domain.Entities;
using Xunit;

namespace StockManufactura.UnitTests;

public class ProductCostSummaryTests
{
    [Fact]
    public async Task GetCostSummaryAsync_DevuelveResumenPorProducto()
    {
        var producto = new Producto
        {
            Nombre = "Producto prueba",
            CostoFabricacionActual = 120m,
            PrecioSugeridoActual = 180m,
            MargenActual = 0.5m
        };

        var servicio = new ProductCostServiceStub(new[] { producto });
        var resumen = await servicio.GetCostSummaryAsync();

        Assert.Single(resumen);
        Assert.Equal("Producto prueba", resumen[0].ProductName);
        Assert.Equal(120m, resumen[0].CosteActual);
    }

    private sealed class ProductCostServiceStub : IProductCostService
    {
        private readonly IReadOnlyList<Producto> _productos;

        public ProductCostServiceStub(IEnumerable<Producto> productos)
        {
            _productos = productos.ToList();
        }

        public Task RecalculateAffectedProductsAsync(ProductRecalculationRequest request, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IReadOnlyList<ProductCostHistory>> GetProductCostHistoryAsync(Guid productId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ProductCostHistory>>(Array.Empty<ProductCostHistory>());

        public Task<IReadOnlyList<ProductCostSnapshot>> GetProductSnapshotsAsync(Guid productId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ProductCostSnapshot>>(Array.Empty<ProductCostSnapshot>());

        public Task<ProductCostComparison> CompareVersionsAsync(Guid olderHistoryId, Guid newerHistoryId, CancellationToken cancellationToken = default)
            => Task.FromResult(new ProductCostComparison());

        public Task<IReadOnlyList<ProductCostSummary>> GetCostSummaryAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ProductCostSummary>>(
                _productos.Select(product => new ProductCostSummary
                {
                    ProductId = product.Id,
                    ProductName = product.Nombre,
                    CosteActual = product.CostoFabricacionActual,
                    PrecioSugerido = product.PrecioSugeridoActual,
                    Margen = product.MargenActual
                }).ToArray());
    }
}
