using System;

namespace StockManufactura.Domain.Entities
{
    public sealed class ResourcePriceHistory : BaseEntity
    {
        public Guid RecursoId { get; set; }
        public Recurso Recurso { get; set; } = null!;
        public DateTime Fecha { get; set; } = DateTime.UtcNow;
        public string Usuario { get; set; } = string.Empty;
        public decimal PrecioAnterior { get; set; }
        public decimal PrecioNuevo { get; set; }
        public Moneda Moneda { get; set; }
        public decimal? CotizacionUtilizada { get; set; }
        public decimal PrecioEquivalentePesos { get; set; }
        public string MotivoCambio { get; set; } = string.Empty;
        public string Observaciones { get; set; } = string.Empty;
    }
}
