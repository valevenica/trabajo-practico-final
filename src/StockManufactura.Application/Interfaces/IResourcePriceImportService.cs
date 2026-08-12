using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using StockManufactura.Application.Resources;

namespace StockManufactura.Application.Interfaces
{
    public interface IResourcePriceImportService
    {
        Task<IReadOnlyList<ResourcePriceImportRow>> ParsearAsync(Stream excelStream, Guid proveedorId, CancellationToken cancellationToken = default);
        Task<ResourcePriceImportResult> AplicarAsync(IReadOnlyList<ResourcePriceImportRow> filas, string usuario, CancellationToken cancellationToken = default);
    }
}
