using System.Threading;
using System.Threading.Tasks;
using StockManufactura.Application.Services;

namespace StockManufactura.Application.Interfaces
{
    public interface ISystemStatusService
    {
        Task<SystemStatusSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);
    }
}
