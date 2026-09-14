using System;

namespace StockManufactura.Domain.Entities
{
    public sealed class BackupSettings : BaseEntity
    {
        public string CarpetaLocal { get; set; } = string.Empty;
        public bool Automatico { get; set; }
        public int MantenerUltimasCopias { get; set; } = 10;
        public DateTime? UltimoBackupAutomatico { get; set; }
        public int IntervaloMinutos { get; set; } = 60;
        public bool GoogleDriveHabilitado { get; set; }
        public string GoogleDriveFolderId { get; set; } = string.Empty;

        /// <summary>
        /// Carpeta local que el cliente de la nube (Google Drive, OneDrive) replica contra el servicio remoto.
        /// Es el punto de intercambio para la sincronizacion bidireccional.
        /// </summary>
        public string CarpetaNube { get; set; } = string.Empty;

        /// <summary>
        /// Nombre del proveedor de nube configurado, solo a efectos informativos en el panel de estado.
        /// </summary>
        public string ProveedorNube { get; set; } = "Google Drive";

        /// <summary>
        /// Si esta activo se descargan tambien las copias que otros equipos subieron a la nube.
        /// Si esta inactivo la sincronizacion es solo de subida.
        /// </summary>
        public bool SincronizacionBidireccional { get; set; } = true;

        public DateTime? UltimaSincronizacionNube { get; set; }
    }
}
