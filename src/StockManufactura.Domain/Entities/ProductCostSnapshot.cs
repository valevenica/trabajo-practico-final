using System;

namespace StockManufactura.Domain.Entities
{
    public sealed class ProductCostSnapshot : BaseEntity
    {
        public Guid ProductId { get; set; }
        public Producto Product { get; set; } = null!;
        public DateTime Fecha { get; set; } = DateTime.UtcNow;
        public decimal CostoTotal { get; set; }
        public decimal CotizacionUtilizada { get; set; }
        public decimal CostoFinal { get; set; }
    }
}
