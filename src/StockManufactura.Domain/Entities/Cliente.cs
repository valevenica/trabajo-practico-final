using System;

namespace StockManufactura.Domain.Entities
{
    /// <summary>
    /// Destinatario de un pedido. Es opcional: un pedido sin cliente se registra como venta a consumidor final.
    /// </summary>
    public sealed class Cliente : BaseEntity
    {
        public string Nombre { get; set; } = string.Empty;

        /// <summary>CUIT, CUIL o DNI. Opcional, pero único cuando se informa.</summary>
        public string Documento { get; set; } = string.Empty;

        public string Telefono { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public string Ciudad { get; set; } = string.Empty;
        public string Provincia { get; set; } = string.Empty;
        public string Observaciones { get; set; } = string.Empty;
        public bool Activo { get; set; } = true;
        public DateTime FechaAlta { get; set; } = DateTime.UtcNow;
    }
}
