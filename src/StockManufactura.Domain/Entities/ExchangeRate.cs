using System;

namespace StockManufactura.Domain.Entities
{
    public sealed class ExchangeRate : BaseEntity
    {
        public decimal Valor { get; set; }
        public DateTime Fecha { get; set; } = DateTime.UtcNow;
        public string Fuente { get; set; } = string.Empty;
        public string Usuario { get; set; } = string.Empty;
        public bool Automatica { get; set; }
    }
}
