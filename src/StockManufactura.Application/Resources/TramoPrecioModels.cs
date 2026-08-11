using System.Collections.Generic;

namespace StockManufactura.Application.Resources
{
    public sealed class TramoPrecioInput
    {
        public decimal? CantidadHasta { get; set; }
        public decimal PrecioPorUnidad { get; set; }
    }

    public sealed class TramoCalculationDetalle
    {
        public int Orden { get; init; }
        public decimal? CantidadHasta { get; init; }
        public decimal PrecioPorUnidad { get; init; }
        public decimal CantidadConsumida { get; init; }
        public decimal Subtotal { get; init; }
    }

    public sealed class TramoCalculationResult
    {
        public decimal ConsumoTotal { get; init; }
        public decimal CostoTotal { get; init; }
        public decimal PrecioPromedio { get; init; }
        public IReadOnlyList<TramoCalculationDetalle> Detalle { get; init; } = new List<TramoCalculationDetalle>();
    }
}
