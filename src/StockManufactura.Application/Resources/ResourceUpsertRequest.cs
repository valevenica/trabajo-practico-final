using System;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Application.Resources
{
    public sealed class ResourceUpsertRequest
    {
        public Guid? ResourceId { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public string UnidadMedida { get; set; } = string.Empty;
        public decimal StockActual { get; set; }
        public decimal StockMinimo { get; set; }
        public decimal Precio { get; set; }
        public Moneda Moneda { get; set; }
        public Guid? ProveedorHabitualId { get; set; }
        public string MotivoCambio { get; set; } = string.Empty;
        public string Observaciones { get; set; } = string.Empty;
        public bool Activo { get; set; } = true;
        public string Usuario { get; set; } = string.Empty;
    }
}
