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
    public sealed class ResourcePricingService : IResourcePricingService
    {
        private readonly IRecursoRepository _recursoRepository;
        private readonly IExchangeRateRepository _exchangeRateRepository;
        private readonly IResourcePriceHistoryRepository _priceHistoryRepository;
        private readonly IRepository<ResourceCostCalculation> _costCalculationRepository;
        private readonly IProductCostService _productCostService;
        private readonly IAuditLogService _auditLogService;
        private readonly IUnitOfWork _unitOfWork;

        public ResourcePricingService(
            IRecursoRepository recursoRepository,
            IExchangeRateRepository exchangeRateRepository,
            IResourcePriceHistoryRepository priceHistoryRepository,
            IRepository<ResourceCostCalculation> costCalculationRepository,
            IProductCostService productCostService,
            IAuditLogService auditLogService,
            IUnitOfWork unitOfWork)
        {
            _recursoRepository = recursoRepository;
            _exchangeRateRepository = exchangeRateRepository;
            _priceHistoryRepository = priceHistoryRepository;
            _costCalculationRepository = costCalculationRepository;
            _productCostService = productCostService;
            _auditLogService = auditLogService;
            _unitOfWork = unitOfWork;
        }

        public async Task<IReadOnlyList<Recurso>> GetResourcesAsync(CancellationToken cancellationToken = default)
        {
            return await _recursoRepository.ListActivosAsync();
        }

        public async Task<Recurso> UpsertResourceAsync(ResourceUpsertRequest request, CancellationToken cancellationToken = default)
        {
            Recurso? resource = null;
            if (request.ResourceId.HasValue)
            {
                resource = await _recursoRepository.GetByIdAsync(request.ResourceId.Value);
            }

            if (resource is null)
            {
                resource = await _recursoRepository.GetByCodigoAsync(request.Codigo);
            }

            var oldPrice = resource?.Precio ?? request.Precio;
            var oldCurrency = resource?.Moneda ?? request.Moneda;

            if (resource is null)
            {
                resource = new Recurso();
                await _recursoRepository.AddAsync(resource);
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
                var rate = request.Moneda == Moneda.USD ? await _exchangeRateRepository.GetLatestAsync() : null;
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

                await _priceHistoryRepository.AddAsync(history);

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
            var resource = await _recursoRepository.GetByIdAsync(recursoId);
            if (resource is null)
            {
                throw new InvalidOperationException("Recurso no encontrado.");
            }

            decimal rateValue = 1m;
            if (resource.Moneda == Moneda.USD)
            {
                var rate = await _exchangeRateRepository.GetLatestAsync();
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

            await _costCalculationRepository.AddAsync(calculation);
            await _unitOfWork.SaveChangesAsync();
            return quote;
        }

        public async Task<IReadOnlyList<ResourcePriceHistory>> GetPriceHistoryAsync(Guid recursoId, CancellationToken cancellationToken = default)
        {
            return await _priceHistoryRepository.ListByResourceAsync(recursoId);
        }
    }
}
