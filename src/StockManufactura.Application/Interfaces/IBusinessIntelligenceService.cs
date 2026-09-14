using System.Threading;
using System.Threading.Tasks;
using StockManufactura.Application.Reports;

namespace StockManufactura.Application.Interfaces
{
    /// <summary>
    /// Herramienta de apoyo a la toma de decisiones: consolida los datos operativos del sistema
    /// en indicadores, series graficables y alertas accionables para el administrador.
    /// </summary>
    public interface IBusinessIntelligenceService
    {
        /// <param name="year">Anio del periodo a analizar; null analiza toda la historia.</param>
        /// <param name="month">Mes del periodo a analizar; null analiza toda la historia.</param>
        Task<BusinessIntelligenceReport> GetReportAsync(int? year, int? month, CancellationToken cancellationToken = default);
    }
}
