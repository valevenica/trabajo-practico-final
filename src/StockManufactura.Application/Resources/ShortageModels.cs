using System;
using System.Collections.Generic;

namespace StockManufactura.Application.Resources
{
    public sealed class ProductShortageRow
    {
        public Guid ProductoId { get; set; }
        public string ProductoNombre { get; set; } = string.Empty;
        public decimal StockActual { get; set; }
        public decimal Reservado { get; set; }
        public decimal Faltante { get; set; }
        public string Motivo { get; set; } = string.Empty;
    }

    public sealed class ResourceShortageRow
    {
        public Guid RecursoId { get; set; }
        public string RecursoNombre { get; set; } = string.Empty;
        public string UnidadMedida { get; set; } = string.Empty;
        public decimal StockActual { get; set; }
        public decimal Faltante { get; set; }
        public string Motivo { get; set; } = string.Empty;
    }

    public sealed class ShortageReport
    {
        public IReadOnlyList<ProductShortageRow> Productos { get; set; } = Array.Empty<ProductShortageRow>();
        public IReadOnlyList<ResourceShortageRow> Insumos { get; set; } = Array.Empty<ResourceShortageRow>();
        public int TotalCount => Productos.Count + Insumos.Count;
    }
}
