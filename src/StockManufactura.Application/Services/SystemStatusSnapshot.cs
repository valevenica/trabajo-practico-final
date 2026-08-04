using System;

namespace StockManufactura.Application.Services
{
    public sealed class SystemStatusSnapshot
    {
        public DateTime? LastBackupAt { get; set; }
        public DateTime? LastDriveSyncAt { get; set; }
        public DateTime? LastDollarUpdateAt { get; set; }
        public bool IsInternetConnected { get; set; }
        public long DatabaseSizeBytes { get; set; }
        public int ProductCount { get; set; }
        public int ResourceCount { get; set; }
        public int CustomerCount { get; set; }
        public string ApplicationVersion { get; set; } = "1.0.0";
        public string LastDollarSource { get; set; } = string.Empty;
        public bool BackupEnabled { get; set; }
        public bool DriveSyncEnabled { get; set; }
        public string BackupStatus { get; set; } = "Sin datos";
        public string CloudProvider { get; set; } = "No configurado";
        public string StatusTone { get; set; } = "Neutral";
    }
}
