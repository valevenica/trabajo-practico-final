using System;

namespace StockManufactura.Domain.Entities
{
    public sealed class ProductCostHistory : BaseEntity
    {
        public Guid ProductId { get; set; }
        public Producto Product { get; set; } = null!;
        public DateTime Fecha { get; set; } = DateTime.UtcNow;
        public string Usuario { get; set; } = string.Empty;
        public decimal CostoAnterior { get; set; }
        public decimal CostoNuevo { get; set; }
        public decimal PrecioSugeridoAnterior { get; set; }
        public decimal PrecioSugeridoNuevo { get; set; }
        public decimal MargenUtilizado { get; set; }
        public decimal CotizacionUtilizada { get; set; }
        public string MotivoRecalculo { get; set; } = string.Empty;
        public string Observaciones { get; set; } = string.Empty;
    }
}
