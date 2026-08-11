using System;

namespace StockManufactura.Domain.Entities
{
    public sealed class RecursoTramoPrecio : BaseEntity
    {
        public Guid RecursoId { get; set; }
        public Recurso Recurso { get; set; } = null!;
        public int Orden { get; set; }
        public decimal? CantidadHasta { get; set; }
        public decimal PrecioPorUnidad { get; set; }
    }
}
