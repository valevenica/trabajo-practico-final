using System;

namespace StockManufactura.Application.Resources
{
    public sealed class StockIntakeLineInput
    {
        public Guid ItemId { get; set; }
        public bool EsProducto { get; set; }
        public decimal Cantidad { get; set; }
    }
}
