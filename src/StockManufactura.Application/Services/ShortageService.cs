using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StockManufactura.Application.Interfaces;
using StockManufactura.Application.Resources;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Application.Services
{
    public sealed class ShortageService : IShortageService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ShortageService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<ShortageReport> GetShortagesAsync(CancellationToken cancellationToken = default)
        {
            var products = await _unitOfWork.Productos.ListActivosAsync();
            var orders = await _unitOfWork.OrdenesProduccion.ListAsync();
            var resources = await _unitOfWork.Recursos.ListActivosAsync();

            var reservedByProduct = orders
                .Where(o => o.Estado == EstadoOrdenProduccion.Borrador || o.Estado == EstadoOrdenProduccion.Planificada)
                .GroupBy(o => o.ProductoId)
                .ToDictionary(g => g.Key, g => g.Sum(o => o.CantidadPlaneada));

            var productShortages = new List<ProductShortageRow>();
            foreach (var product in products)
            {
                if (!reservedByProduct.TryGetValue(product.Id, out var reserved))
                {
                    continue;
                }

                var faltante = reserved - product.StockActual;
                if (faltante <= 0)
                {
                    continue;
                }

                productShortages.Add(new ProductShortageRow
                {
                    ProductoId = product.Id,
                    ProductoNombre = product.Nombre,
                    StockActual = product.StockActual,
                    Reservado = reserved,
                    Faltante = faltante
                });
            }

            var neededByResource = new Dictionary<Guid, decimal>();
            foreach (var shortage in productShortages)
            {
                var recipeItems = await _unitOfWork.RecetaProductoItems.ListByProductIdAsync(shortage.ProductoId);
                foreach (var item in recipeItems.Where(x => x.RecursoId.HasValue))
                {
                    var qty = item.Cantidad * shortage.Faltante;
                    neededByResource[item.RecursoId!.Value] = neededByResource.GetValueOrDefault(item.RecursoId!.Value) + qty;
                }
            }

            var resourceShortages = new List<ResourceShortageRow>();
            foreach (var resource in resources.Where(r => !r.EsServicio))
            {
                if (neededByResource.TryGetValue(resource.Id, out var needed) && needed > resource.StockActual)
                {
                    resourceShortages.Add(new ResourceShortageRow
                    {
                        RecursoId = resource.Id,
                        RecursoNombre = resource.Nombre,
                        UnidadMedida = resource.UnidadMedida,
                        StockActual = resource.StockActual,
                        Faltante = needed - resource.StockActual,
                        Motivo = "Insuficiente para completar órdenes en espera"
                    });
                    continue;
                }

                if (resource.StockActual < resource.StockMinimo)
                {
                    resourceShortages.Add(new ResourceShortageRow
                    {
                        RecursoId = resource.Id,
                        RecursoNombre = resource.Nombre,
                        UnidadMedida = resource.UnidadMedida,
                        StockActual = resource.StockActual,
                        Faltante = resource.StockMinimo - resource.StockActual,
                        Motivo = "Por debajo del stock mínimo"
                    });
                }
            }

            return new ShortageReport
            {
                Productos = productShortages,
                Insumos = resourceShortages
            };
        }
    }
}
