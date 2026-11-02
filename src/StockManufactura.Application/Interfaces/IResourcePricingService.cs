using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using StockManufactura.Application.Resources;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Application.Interfaces
{
    public interface IResourcePricingService
    {
        Task<IReadOnlyList<Recurso>> GetResourcesAsync(CancellationToken cancellationToken = default);
        /// <summary>Incluye los insumos desactivados, para la pantalla de gestión que permite reactivarlos.</summary>
        Task<IReadOnlyList<Recurso>> GetAllResourcesAsync(CancellationToken cancellationToken = default);
        Task<Recurso> UpsertResourceAsync(ResourceUpsertRequest request, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ResourcePriceHistory>> GetPriceHistoryAsync(Guid recursoId, CancellationToken cancellationToken = default);
        Task<int> RecalcularTodosUSDAsync(string usuario, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<RecursoTramoPrecio>> GetTramosAsync(Guid recursoId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<RecursoTramoPrecio>> GuardarTramosAsync(Guid recursoId, IReadOnlyList<TramoPrecioInput> tramos, string usuario, CancellationToken cancellationToken = default);
    }
}
