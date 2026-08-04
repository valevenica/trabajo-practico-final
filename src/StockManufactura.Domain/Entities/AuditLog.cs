using System;

namespace StockManufactura.Domain.Entities
{
    public sealed class AuditLog : BaseEntity
    {
        public DateTime FechaHora { get; set; } = DateTime.UtcNow;
        public string Usuario { get; set; } = string.Empty;
        public string Modulo { get; set; } = string.Empty;
        public string Accion { get; set; } = string.Empty;
        public string Entidad { get; set; } = string.Empty;
        public string IdEntidad { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string Equipo { get; set; } = Environment.MachineName;
    }
}
