using System;

namespace StockManufactura.Domain.Entities
{
    public sealed class RecursoProveedor : BaseEntity
    {
        public Guid RecursoId { get; set; }
        public Recurso Recurso { get; set; } = null!;
        public Guid ProveedorId { get; set; }
        public Proveedor Proveedor { get; set; } = null!;
        public decimal Precio { get; set; }
        public bool EsPrioritario { get; set; }
        public string Observaciones { get; set; } = string.Empty;
    }
}
