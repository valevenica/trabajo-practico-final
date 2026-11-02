using System;
using System.Threading;
using System.Threading.Tasks;

namespace StockManufactura.Application.Interfaces
{
    /// <summary>
    /// Productos e insumos no se eliminan: se desactivan para ocultarlos de los listados y de las altas nuevas
    /// (recetas, órdenes, ingresos de stock), conservando el historial de costos, órdenes y auditoría que los referencia.
    /// </summary>
    public interface ICatalogActivationService
    {
        Task<CatalogActivationResult> SetProductoActivoAsync(Guid productoId, bool activo, string usuario, string equipo, CancellationToken cancellationToken = default);
        Task<CatalogActivationResult> SetRecursoActivoAsync(Guid recursoId, bool activo, string usuario, string equipo, CancellationToken cancellationToken = default);
    }

    public sealed record CatalogActivationResult(bool Activo, string Mensaje);
}
