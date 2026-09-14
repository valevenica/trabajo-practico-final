using System;
using System.Collections.Generic;

namespace StockManufactura.Application.Reports
{
    /// <summary>
    /// Punto de una serie graficable. <see cref="Escala"/> es el valor normalizado entre 0 y 1
    /// dentro de su serie, para que la vista pueda dibujar la barra sin calcular nada.
    /// </summary>
    public sealed class ChartPoint
    {
        public string Etiqueta { get; init; } = string.Empty;
        public decimal Valor { get; init; }
        public string ValorFormateado { get; init; } = string.Empty;
        public double Escala { get; set; }
        public string Detalle { get; init; } = string.Empty;
    }

    /// <summary>
    /// Recomendacion derivada de los datos del sistema, pensada para dar contexto
    /// a quien tiene que tomar una decision (comprar, recalcular precios, revisar margenes).
    /// </summary>
    public sealed class DecisionAlert
    {
        public string Severidad { get; init; } = "Info";
        public string Titulo { get; init; } = string.Empty;
        public string Detalle { get; init; } = string.Empty;
        public string Recomendacion { get; init; } = string.Empty;
    }

    public sealed class BusinessIntelligenceReport
    {
        public DateTime GeneradoUtc { get; init; } = DateTime.UtcNow;
        public string PeriodoDescripcion { get; init; } = string.Empty;

        // Indicadores clave.
        public int ProductosActivos { get; init; }
        public int InsumosActivos { get; init; }
        public decimal CostoPromedioProducto { get; init; }
        public decimal MargenPromedio { get; init; }
        public decimal ValorInventarioInsumos { get; init; }
        public decimal ValorInventarioProductos { get; init; }
        public int OrdenesFinalizadas { get; init; }
        public int OrdenesEnProceso { get; init; }
        public decimal ProduccionValorizada { get; init; }
        public decimal VariacionCostoPromedioPct { get; init; }
        public decimal CotizacionVigente { get; init; }

        // Series graficables.
        public IReadOnlyList<ChartPoint> CostoPorProducto { get; init; } = Array.Empty<ChartPoint>();
        public IReadOnlyList<ChartPoint> MayoresAumentos { get; init; } = Array.Empty<ChartPoint>();
        public IReadOnlyList<ChartPoint> OrdenesPorEstado { get; init; } = Array.Empty<ChartPoint>();
        public IReadOnlyList<ChartPoint> EvolucionCosto { get; init; } = Array.Empty<ChartPoint>();
        public IReadOnlyList<ChartPoint> ActividadPorModulo { get; init; } = Array.Empty<ChartPoint>();
        public IReadOnlyList<ChartPoint> InsumosPorValorInmovilizado { get; init; } = Array.Empty<ChartPoint>();

        public IReadOnlyList<DecisionAlert> Alertas { get; init; } = Array.Empty<DecisionAlert>();
    }
}
