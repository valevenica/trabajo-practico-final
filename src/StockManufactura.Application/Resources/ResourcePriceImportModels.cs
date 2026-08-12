using System;

namespace StockManufactura.Application.Resources
{
    public enum ResourcePriceImportRowStatus
    {
        Ok,
        SinCambios,
        NoEncontrado,
        PrecioInvalido
    }

    public sealed class ResourcePriceImportRow
    {
        public int Fila { get; set; }
        public string CodigoProveedor { get; set; } = string.Empty;
        public string PrecioTexto { get; set; } = string.Empty;
        public decimal? Precio { get; set; }
        public Guid? RecursoProveedorId { get; set; }
        public Guid? RecursoId { get; set; }
        public string? RecursoNombre { get; set; }
        public decimal? PrecioActual { get; set; }
        public ResourcePriceImportRowStatus Status { get; set; }
        public string Mensaje { get; set; } = string.Empty;
    }

    public sealed class ResourcePriceImportResult
    {
        public int Actualizados { get; set; }
        public int Omitidos { get; set; }
    }
}
