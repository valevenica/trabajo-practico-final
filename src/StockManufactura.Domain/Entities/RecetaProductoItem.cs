using System;

namespace StockManufactura.Domain.Entities
{
    public sealed class RecetaProductoItem : BaseEntity
    {
        public Guid ProductoId { get; set; }
        public Producto Producto { get; set; } = null!;
        public Guid RecursoId { get; set; }
        public Recurso Recurso { get; set; } = null!;
        public decimal Cantidad { get; set; }
        public decimal CostoParcialManual { get; set; }
        public string Observaciones { get; set; } = string.Empty;
    }
}
