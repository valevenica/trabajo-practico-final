using System;
using System.Collections.Generic;

namespace StockManufactura.Application.Backups
{
    /// <summary>
    /// Resultado de una pasada de sincronizacion bidireccional contra el servicio en la nube.
    /// </summary>
    public sealed class CloudSyncResult
    {
        public bool Ejecutada { get; init; }
        public string Proveedor { get; init; } = string.Empty;
        public DateTime FechaUtc { get; init; } = DateTime.UtcNow;
        public IReadOnlyList<string> Subidos { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> Descargados { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> Conflictos { get; init; } = Array.Empty<string>();
        public string Mensaje { get; init; } = string.Empty;

        public int TotalSubidos => Subidos.Count;
        public int TotalDescargados => Descargados.Count;
        public int TotalConflictos => Conflictos.Count;

        public static CloudSyncResult NoEjecutada(string motivo) => new()
        {
            Ejecutada = false,
            Mensaje = motivo
        };
    }
}
