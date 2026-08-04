using System;
using System.Collections.Generic;

namespace StockManufactura.Application.Products
{
    public sealed class ProductCostComparison
    {
        public Guid ProductId { get; set; }
        public DateTime FechaAnterior { get; set; }
        public DateTime FechaNueva { get; set; }
        public decimal CostoAnterior { get; set; }
        public decimal CostoNuevo { get; set; }
        public decimal VariacionAbsoluta { get; set; }
        public decimal VariacionPorcentual { get; set; }
        public decimal CotizacionUtilizada { get; set; }
        public IReadOnlyList<string> RecursosModificados { get; set; } = Array.Empty<string>();
    }
}
