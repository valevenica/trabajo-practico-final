using System.Threading;
using System.Threading.Tasks;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Application.Interfaces
{
    public interface IExchangeRateProvider
    {
        string Key { get; }
        string DisplayName { get; }
        Task<ExchangeRate> GetCurrentRateAsync(string usuario, CancellationToken cancellationToken = default);
    }
}
