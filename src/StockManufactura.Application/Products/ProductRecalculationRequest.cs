using System;

namespace StockManufactura.Application.Products
{
    public sealed class ProductRecalculationRequest
    {
        public string Usuario { get; set; } = string.Empty;
        public string Motivo { get; set; } = string.Empty;
        public Guid? RecursoDisparadorId { get; set; }
        public Guid? ProductoDisparadorId { get; set; }
        public bool CambioCotizacion { get; set; }
        public bool CambioReceta { get; set; }
        public bool CambioCostoIndirecto { get; set; }
    }
}
