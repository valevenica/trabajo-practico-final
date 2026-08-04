using System;

namespace StockManufactura.Domain.Entities
{
    public sealed class BackupRecord : BaseEntity
    {
        public DateTime FechaHora { get; set; } = DateTime.UtcNow;
        public string Tipo { get; set; } = "Manual";
        public string RutaArchivo { get; set; } = string.Empty;
        public long TamanoBytes { get; set; }
        public bool Exitoso { get; set; }
        public string Mensaje { get; set; } = string.Empty;
    }
}
