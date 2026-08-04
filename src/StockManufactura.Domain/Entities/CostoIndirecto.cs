using System;

namespace StockManufactura.Domain.Entities
{
    public sealed class CostoIndirecto : BaseEntity
    {
        public string Nombre { get; set; } = string.Empty;
        public decimal ValorMensual { get; set; }
        public decimal FactorAplicacion { get; set; } = 1m;
        public bool Activo { get; set; } = true;
        public string Observaciones { get; set; } = string.Empty;
    }
}
