using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using StockManufactura.Application.Resources;

namespace StockManufactura.Application.Interfaces
{
    public interface IStockIntakeService
    {
        Task RegistrarIngresoAsync(IReadOnlyList<StockIntakeLineInput> lineas, string usuario, CancellationToken cancellationToken = default);
    }
}
