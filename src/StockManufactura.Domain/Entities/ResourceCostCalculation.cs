using System;

namespace StockManufactura.Domain.Entities
{
    public sealed class ResourceCostCalculation : BaseEntity
    {
        public Guid RecursoId { get; set; }
        public Recurso Recurso { get; set; } = null!;
        public DateTime FechaCalculo { get; set; } = DateTime.UtcNow;
        public decimal CotizacionUtilizada { get; set; }
        public decimal CostoEnPesos { get; set; }
    }
}
