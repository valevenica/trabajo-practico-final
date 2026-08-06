using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using StockManufactura.Application.Interfaces;
using StockManufactura.Application.Resources;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Application.Services
{
    public sealed class ResourcePricingService : IResourcePricingService
    {
        private readonly IProductCostService _productCostService;
        private readonly IAuditLogService _auditLogService;
        private readonly IUnitOfWork _unitOfWork;

        public ResourcePricingService(
            IProductCostService productCostService,
            IAuditLogService auditLogService,
            IUnitOfWork unitOfWork)
        {
            _productCostService = productCostService;
            _auditLogService = auditLogService;
            _unitOfWork = unitOfWork;
        }

        public async Task<IReadOnlyList<Recurso>> GetResourcesAsync(CancellationToken cancellationToken = default)
        {
            return await _unitOfWork.Recursos.ListActivosAsync();
        }

        public async Task<Recurso> UpsertResourceAsync(ResourceUpsertRequest request, CancellationToken cancellationToken = default)
        {
            Recurso? resource = null;
            if (request.ResourceId.HasValue)
            {
                resource = await _unitOfWork.Recursos.GetByIdAsync(request.ResourceId.Value);
            }

            if (resource is null)
            {
                resource = await _unitOfWork.Recursos.GetByCodigoAsync(request.Codigo);
            }

            var oldPrice = resource?.Precio ?? request.Precio;
            var oldCurrency = resource?.Moneda ?? request.Moneda;

            if (resource is null)
            {
                resource = new Recurso();
                await _unitOfWork.Recursos.AddAsync(resource);
            }

            resource.Codigo = request.Codigo;
            resource.Nombre = request.Nombre;
            resource.Descripcion = request.Descripcion;
            resource.Categoria = request.Categoria;
            resource.UnidadMedida = request.UnidadMedida;
            resource.StockActual = request.StockActual;
            resource.StockMinimo = request.StockMinimo;
            resource.Precio = request.Precio;
            resource.Moneda = request.Moneda;
            resource.ProveedorHabitualId = request.ProveedorHabitualId;
            resource.Observaciones = request.Observaciones;
            resource.Activo = request.Activo;
            resource.FechaUltimaActualizacion = DateTime.UtcNow;

            if (oldPrice != request.Precio || oldCurrency != request.Moneda)
            {
                var rate = request.Moneda == Moneda.USD ? await _unitOfWork.ExchangeRates.GetLatestAsync() : null;
                var rateValue = rate?.Valor ?? 1m;
                var equivalentArs = request.Moneda == Moneda.USD ? request.Precio * rateValue : request.Precio;
                var history = new ResourcePriceHistory
                {
                    Recurso = resource,
                    RecursoId = resource.Id,
                    Fecha = DateTime.UtcNow,
                    Usuario = request.Usuario,
                    PrecioAnterior = oldPrice,
                    PrecioNuevo = request.Precio,
                    Moneda = request.Moneda,
                    CotizacionUtilizada = rate?.Valor,
                    PrecioEquivalentePesos = equivalentArs,
                    MotivoCambio = request.MotivoCambio,
                    Observaciones = request.Observaciones
                };

                await _unitOfWork.ResourcePriceHistory.AddAsync(history);

                await _auditLogService.RegisterAsync(new AuditLog
                {
                    FechaHora = DateTime.UtcNow,
                    Usuario = request.Usuario,
                    Modulo = "Recursos",
                    Accion = "CambioPrecio",
                    Entidad = nameof(Recurso),
                    IdEntidad = resource.Id.ToString(),
                    Descripcion = $"Precio actualizado de {oldPrice} a {request.Precio} ({request.Moneda})",
                    Equipo = Environment.MachineName
                });

                await _productCostService.RecalculateAffectedProductsAsync(new Products.ProductRecalculationRequest
                {
                    Usuario = request.Usuario,
                    Motivo = string.IsNullOrWhiteSpace(request.MotivoCambio) ? "Cambio de precio de recurso" : request.MotivoCambio,
                    RecursoDisparadorId = resource.Id
                });
            }

            await _unitOfWork.SaveChangesAsync();
            return resource;
        }

        public async Task<ResourceCostQuote> CalculateCostAsync(Guid recursoId, CancellationToken cancellationToken = default)
        {
            var resource = await _unitOfWork.Recursos.GetByIdAsync(recursoId);
            if (resource is null)
            {
                throw new InvalidOperationException("Recurso no encontrado.");
            }

            decimal rateValue = 1m;
            if (resource.Moneda == Moneda.USD)
            {
                var rate = await _unitOfWork.ExchangeRates.GetLatestAsync();
                if (rate is null)
                {
                    throw new InvalidOperationException("No hay cotización registrada para convertir USD a ARS.");
                }
                rateValue = rate.Valor;
            }

            var quote = new ResourceCostQuote
            {
                RecursoId = resource.Id,
                Moneda = resource.Moneda,
                PrecioOriginal = resource.Precio,
                CotizacionUtilizada = rateValue,
                CostoEnPesos = resource.Moneda == Moneda.USD ? resource.Precio * rateValue : resource.Precio,
                FechaCalculo = DateTime.UtcNow
            };

            var calculation = new ResourceCostCalculation
            {
                RecursoId = resource.Id,
                Recurso = resource,
                FechaCalculo = quote.FechaCalculo,
                CotizacionUtilizada = quote.CotizacionUtilizada,
                CostoEnPesos = quote.CostoEnPesos
            };

            await _unitOfWork.ResourceCostCalculations.AddAsync(calculation);
            await _unitOfWork.SaveChangesAsync();
            return quote;
        }

        public async Task<IReadOnlyList<ResourcePriceHistory>> GetPriceHistoryAsync(Guid recursoId, CancellationToken cancellationToken = default)
        {
            return await _unitOfWork.ResourcePriceHistory.ListByResourceAsync(recursoId);
        }

        public async Task<int> RecalcularTodosUSDAsync(string usuario, CancellationToken cancellationToken = default)
        {
            var rate = await _unitOfWork.ExchangeRates.GetLatestAsync();
            if (rate is null) return 0;

            var recursos = await _unitOfWork.Recursos.ListActivosAsync();
            var usdResources = recursos.Where(x => x.Moneda == Moneda.USD).ToList();

            foreach (var recurso in usdResources)
            {
                var request = new ResourceUpsertRequest
                {
                    ResourceId = recurso.Id,
                    Codigo = recurso.Codigo,
                    Nombre = recurso.Nombre,
                    Descripcion = recurso.Descripcion,
                    Categoria = recurso.Categoria,
                    UnidadMedida = recurso.UnidadMedida,
                    StockActual = recurso.StockActual,
                    StockMinimo = recurso.StockMinimo,
                    Precio = recurso.Precio,
                    Moneda = recurso.Moneda,
                    ProveedorHabitualId = recurso.ProveedorHabitualId,
                    Observaciones = recurso.Observaciones,
                    Activo = recurso.Activo,
                    MotivoCambio = $"Recálculo masivo USD @ cotización {rate.Valor:0.00}",
                    Usuario = usuario
                };
                await UpsertResourceAsync(request, cancellationToken);
            }

            return usdResources.Count;
        }
    }
}
