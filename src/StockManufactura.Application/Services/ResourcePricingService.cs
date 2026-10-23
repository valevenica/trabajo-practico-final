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
            resource.EsServicio = request.EsServicio;
            resource.StockActual = request.EsServicio ? 0 : request.StockActual;
            resource.StockMinimo = request.EsServicio ? 0 : request.StockMinimo;
            resource.Precio = request.Precio;
            resource.Moneda = request.Moneda;
            resource.ProveedorHabitualId = request.ProveedorHabitualId;
            resource.Observaciones = request.Observaciones;
            resource.Activo = request.Activo;
            resource.FechaUltimaActualizacion = DateTime.UtcNow;

            var precioCambio = oldPrice != request.Precio || oldCurrency != request.Moneda;
            if (precioCambio)
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

            }

            await _unitOfWork.SaveChangesAsync();

            // El recálculo usa su propio contexto y lee el precio desde la base: debe correr después de guardarlo, o los
            // productos quedarían costeados con el precio anterior.
            if (precioCambio)
            {
                await _productCostService.RecalculateAffectedProductsAsync(new Products.ProductRecalculationRequest
                {
                    Usuario = request.Usuario,
                    Motivo = string.IsNullOrWhiteSpace(request.MotivoCambio) ? "Cambio de precio de recurso" : request.MotivoCambio,
                    RecursoDisparadorId = resource.Id
                });
            }

            return resource;
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
                    EsServicio = recurso.EsServicio,
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

            // El precio en dólares no cambia, cambia la cotización: se recalculan todos los productos para valorizar
            // sus insumos importados con la cotización vigente.
            if (usdResources.Count > 0)
            {
                await _productCostService.RecalculateAffectedProductsAsync(new Products.ProductRecalculationRequest
                {
                    Usuario = usuario,
                    Motivo = $"Recálculo masivo USD @ cotización {rate.Valor:0.00}",
                    CambioCotizacion = true
                }, cancellationToken);
            }

            return usdResources.Count;
        }

        public async Task<IReadOnlyList<RecursoTramoPrecio>> GetTramosAsync(Guid recursoId, CancellationToken cancellationToken = default)
        {
            return await _unitOfWork.RecursoTramosPrecio.ListByRecursoIdAsync(recursoId);
        }

        public async Task<IReadOnlyList<RecursoTramoPrecio>> GuardarTramosAsync(Guid recursoId, IReadOnlyList<TramoPrecioInput> tramos, string usuario, CancellationToken cancellationToken = default)
        {
            var resource = await _unitOfWork.Recursos.GetByIdAsync(recursoId);
            if (resource is null)
            {
                throw new InvalidOperationException("Recurso no encontrado.");
            }

            await _unitOfWork.RecursoTramosPrecio.DeleteByRecursoIdAsync(recursoId);

            var orden = 1;
            var guardados = new List<RecursoTramoPrecio>();
            foreach (var tramo in tramos)
            {
                if (tramo.PrecioPorUnidad < 0)
                {
                    throw new InvalidOperationException("El precio por unidad no puede ser negativo.");
                }

                var entity = new RecursoTramoPrecio
                {
                    RecursoId = recursoId,
                    Orden = orden++,
                    CantidadHasta = tramo.CantidadHasta,
                    PrecioPorUnidad = tramo.PrecioPorUnidad
                };
                await _unitOfWork.RecursoTramosPrecio.AddAsync(entity);
                guardados.Add(entity);
            }

            await _unitOfWork.SaveChangesAsync();

            // Los tramos son el precio efectivo de un insumo de servicio; sin este recalculo,
            // el costo de fabricacion y los snapshots de los productos que lo usan quedan
            // desactualizados hasta el proximo recalculo disparado por otra causa.
            await _productCostService.RecalculateAffectedProductsAsync(new Products.ProductRecalculationRequest
            {
                Usuario = usuario,
                Motivo = "Actualización de tramos de precio del insumo",
                RecursoDisparadorId = recursoId
            }, cancellationToken);

            return guardados;
        }

    }
}
