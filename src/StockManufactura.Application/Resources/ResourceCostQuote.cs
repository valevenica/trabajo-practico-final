using System;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Application.Resources
{
    public sealed class ResourceCostQuote
    {
        public Guid RecursoId { get; set; }
        public Moneda Moneda { get; set; }
        public decimal PrecioOriginal { get; set; }
        public decimal CotizacionUtilizada { get; set; }
        public decimal CostoEnPesos { get; set; }
        public DateTime FechaCalculo { get; set; }
    }
}
