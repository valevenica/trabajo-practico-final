using System;

namespace StockManufactura.Application.Backups
{
    /// <summary>
    /// Copia de seguridad tal como se ve en el lado de la nube, con la huella que permite
    /// decidir si es la misma copia que la local o una divergencia.
    /// </summary>
    public sealed class CloudBackupItem
    {
        public string NombreArchivo { get; init; } = string.Empty;
        public string RutaCompleta { get; init; } = string.Empty;
        public long TamanoBytes { get; init; }
        public DateTime FechaUtc { get; init; }
        public string Hash { get; init; } = string.Empty;
    }
}
