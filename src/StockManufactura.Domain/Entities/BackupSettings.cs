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
    }
}
