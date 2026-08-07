using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using StockManufactura.Application.Interfaces;
using StockManufactura.Application.Monetary;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Application.Services
{
    public sealed class MonetaryConfigurationService : IMonetaryConfigurationService
    {
        private readonly IProductCostService _productCostService;
        private readonly IAuditLogService _auditLogService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IReadOnlyDictionary<string, IExchangeRateProvider> _providers;

        public MonetaryConfigurationService(
            IProductCostService productCostService,
            IAuditLogService auditLogService,
            IUnitOfWork unitOfWork,
            IEnumerable<IExchangeRateProvider> providers)
        {
            _productCostService = productCostService;
            _auditLogService = auditLogService;
            _unitOfWork = unitOfWork;
            _providers = providers.ToDictionary(x => x.Key, StringComparer.OrdinalIgnoreCase);
        }

        public IReadOnlyCollection<IExchangeRateProvider> GetProviders() => _providers.Values.OrderBy(x => x.DisplayName).ToArray();

        public async Task<MonetaryConfigurationState> GetCurrentStateAsync(CancellationToken cancellationToken = default)
        {
            var latest = await _unitOfWork.ExchangeRates.GetLatestAsync();
            if (latest is null)
            {
                return new MonetaryConfigurationState
                {
                    CurrentRate = 0,
                    LastUpdate = DateTime.MinValue,
                    Source = "Sin cotización"
                };
            }

            return new MonetaryConfigurationState
            {
                CurrentRate = latest.Valor,
                LastUpdate = latest.Fecha,
                Source = latest.Fuente
            };
        }

        public async Task<ExchangeRate> UpdateManualAsync(decimal value, string fuente, string usuario, CancellationToken cancellationToken = default)
        {
            var rate = new ExchangeRate
            {
                Valor = value,
                Fecha = DateTime.UtcNow,
                Fuente = fuente,
                Usuario = usuario,
                Automatica = false
            };

            await _unitOfWork.ExchangeRates.AddAsync(rate);
            await _unitOfWork.SaveChangesAsync();

            await _auditLogService.RegisterAsync(new AuditLog
            {
                FechaHora = DateTime.UtcNow,
                Usuario = usuario,
                Modulo = "ConfiguracionMonetaria",
                Accion = "CambioCotizacionManual",
                Entidad = nameof(ExchangeRate),
                IdEntidad = rate.Id.ToString(),
                Descripcion = $"Cotizacion manual registrada: {value}",
                Equipo = Environment.MachineName
            });

            try
            {
                await _productCostService.RecalculateAffectedProductsAsync(new Products.ProductRecalculationRequest
                {
                    Usuario = usuario,
                    Motivo = "Cambio manual de cotizacion",
                    CambioCotizacion = true
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Recalculo de productos tras cambio manual de cotizacion fallo");
            }

            return rate;
        }

        public async Task<ExchangeRate> UpdateAutomaticAsync(string providerKey, string usuario, CancellationToken cancellationToken = default)
        {
            if (!_providers.TryGetValue(providerKey, out var provider))
            {
                throw new InvalidOperationException($"No existe el proveedor de cotización '{providerKey}'.");
            }

            ExchangeRate rate;

            try
            {
                rate = await provider.GetCurrentRateAsync(usuario, cancellationToken);
                rate.Automatica = true;
                await _unitOfWork.ExchangeRates.AddAsync(rate);
                await _unitOfWork.SaveChangesAsync();

                await _auditLogService.RegisterAsync(new AuditLog
                {
                    FechaHora = DateTime.UtcNow,
                    Usuario = usuario,
                    Modulo = "ConfiguracionMonetaria",
                    Accion = "CambioCotizacionAutomatica",
                    Entidad = nameof(ExchangeRate),
                    IdEntidad = rate.Id.ToString(),
                    Descripcion = $"Cotizacion actualizada desde {provider.DisplayName}: {rate.Valor}",
                    Equipo = Environment.MachineName
                });
            }
            catch
            {
                var latest = await _unitOfWork.ExchangeRates.GetLatestAsync();
                if (latest is null)
                {
                    throw;
                }

                rate = latest;
                await _auditLogService.RegisterAsync(new AuditLog
                {
                    FechaHora = DateTime.UtcNow,
                    Usuario = usuario,
                    Modulo = "ConfiguracionMonetaria",
                    Accion = "CambioCotizacionFallback",
                    Entidad = nameof(ExchangeRate),
                    IdEntidad = latest.Id.ToString(),
                    Descripcion = "Sin conexion. Se utiliza ultima cotizacion almacenada.",
                    Equipo = Environment.MachineName
                });
            }

            try
            {
                await _productCostService.RecalculateAffectedProductsAsync(new Products.ProductRecalculationRequest
                {
                    Usuario = usuario,
                    Motivo = "Actualizacion de cotizacion",
                    CambioCotizacion = true
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Recalculo de productos tras actualizacion de cotizacion fallo");
            }

            return rate;
        }

        public async Task<IReadOnlyList<ExchangeRate>> GetHistoryAsync(CancellationToken cancellationToken = default)
        {
            var history = await _unitOfWork.ExchangeRates.ListAsync();
            return history.OrderByDescending(x => x.Fecha).ToArray();
        }

        public async Task SetPrioritariaAsync(Guid id, string usuario, CancellationToken cancellationToken = default)
        {
            await _unitOfWork.ExchangeRates.SetPrioritariaAsync(id);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
