using System;

namespace StockManufactura.Domain.Entities
{
    public sealed class ProductCostSnapshotItem : BaseEntity
    {
        public Guid SnapshotId { get; set; }
        public ProductCostSnapshot Snapshot { get; set; } = null!;
        public Guid RecursoId { get; set; }
        public Recurso Recurso { get; set; } = null!;
        public decimal CantidadUtilizada { get; set; }
        public decimal PrecioRecurso { get; set; }
        public decimal CotizacionUtilizada { get; set; }
        public decimal CostoParcial { get; set; }
    }
}
