using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using StockManufactura.Application.Monetary;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Application.Interfaces
{
    public interface IMonetaryConfigurationService
    {
        IReadOnlyCollection<IExchangeRateProvider> GetProviders();
        Task<MonetaryConfigurationState> GetCurrentStateAsync(CancellationToken cancellationToken = default);
        Task<ExchangeRate> UpdateManualAsync(decimal value, string fuente, string usuario, CancellationToken cancellationToken = default);
        Task<ExchangeRate> UpdateAutomaticAsync(string providerKey, string usuario, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ExchangeRate>> GetHistoryAsync(CancellationToken cancellationToken = default);
    }
}
