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
        public int CriticalStockCount { get; set; }
        public int OrdersInProcessCount { get; set; }
        public decimal MonthlyCostTotal { get; set; }
        public decimal MonthlySalesTotal { get; set; }

        // Indicadores comerciales del periodo. Las ventas son los pedidos finalizados valorizados al precio
        // sugerido vigente del producto, y su costo, al costo de fabricacion vigente.
        public int OrdersInPeriodCount { get; set; }
        public int OpenOrdersCount { get; set; }
        public int FinishedOrdersInPeriodCount { get; set; }
        public decimal AverageTicket { get; set; }
        public decimal MonthlySalesCost { get; set; }

        /// <summary>Ganancia bruta sobre ventas, en porcentaje; nulo si no hubo ventas.</summary>
        public decimal? GrossMarginPercent { get; set; }

        public decimal? PreviousPeriodSalesTotal { get; set; }

        /// <summary>Variacion porcentual de las ventas contra el mes anterior; nula sin base de comparacion.</summary>
        public decimal? SalesVariationPercent { get; set; }

        // Indicadores de trazabilidad y auditoria del periodo seleccionado.
        public int AuditEventCount { get; set; }
        public int AuditUserCount { get; set; }
        public DateTime? LastAuditAt { get; set; }
        public string LastAuditDescription { get; set; } = string.Empty;
        public int PriceChangeCount { get; set; }
        public int CostRecalculationCount { get; set; }

        /// <summary>
        /// Porcentaje de productos activos que tienen al menos un registro de historial de costos,
        /// es decir, cuya evolucion de costo se puede reconstruir.
        /// </summary>
        public decimal TraceabilityCoveragePercent { get; set; }

        // Estado de la sincronizacion con la nube.
        public bool CloudBidirectionalEnabled { get; set; }
        public string CloudSyncStatus { get; set; } = "No configurado";
    }
}
