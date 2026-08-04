using System;

namespace StockManufactura.Domain.Entities
{
    public sealed class Producto : BaseEntity
    {
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public decimal CostoFabricacionActual { get; set; }
        public decimal MargenActual { get; set; }
        public decimal PrecioSugeridoActual { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaUltimoCalculo { get; set; } = DateTime.UtcNow;
        public string Observaciones { get; set; } = string.Empty;
    }
}
