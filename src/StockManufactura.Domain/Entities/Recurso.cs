using System;

namespace StockManufactura.Domain.Entities
{
    public sealed class Recurso : BaseEntity
    {
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public string UnidadMedida { get; set; } = string.Empty;
        public decimal StockActual { get; set; }
        public decimal StockMinimo { get; set; }
        public decimal Precio { get; set; }
        public Moneda Moneda { get; set; } = Moneda.ARS;
        public Guid? ProveedorHabitualId { get; set; }
        public Proveedor? ProveedorHabitual { get; set; }
        public DateTime FechaUltimaActualizacion { get; set; } = DateTime.UtcNow;
        public string Observaciones { get; set; } = string.Empty;
        public bool Activo { get; set; } = true;
        public bool EsServicio { get; set; }

        /// <summary>
        /// Precio por unidad en pesos: los insumos cotizados en dólares se convierten con la cotización vigente. Es el
        /// valor que usan el costo de fabricación, la valorización del inventario y la pantalla de productos.
        /// </summary>
        public decimal PrecioEnPesos(decimal cotizacion) => Moneda == Moneda.USD ? Precio * cotizacion : Precio;
    }
}
