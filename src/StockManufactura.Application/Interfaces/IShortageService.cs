using System.Threading;
using System.Threading.Tasks;
using StockManufactura.Application.Resources;

namespace StockManufactura.Application.Interfaces
{
    public interface IShortageService
    {
        Task<ShortageReport> GetShortagesAsync(CancellationToken cancellationToken = default);
    }
}
