using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StockManufactura.Application.Interfaces;
using StockManufactura.Domain.Entities;
using StockManufactura.Shared;

namespace StockManufactura.Application.Services
{
    public sealed class SystemStatusService : ISystemStatusService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMonetaryConfigurationService _monetaryConfigurationService;
        private readonly IBackupService _backupService;
        private readonly IGoogleDriveBackupSyncService? _cloudSyncService;

        public SystemStatusService(
            IUnitOfWork unitOfWork,
            IMonetaryConfigurationService monetaryConfigurationService,
            IBackupService backupService,
            IGoogleDriveBackupSyncService? cloudSyncService = null)
        {
            _unitOfWork = unitOfWork;
            _monetaryConfigurationService = monetaryConfigurationService;
            _backupService = backupService;
            _cloudSyncService = cloudSyncService;
        }

        public async Task<SystemStatusSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
        {
            var products = await _unitOfWork.Productos.ListAsync();
            var resources = await _unitOfWork.Recursos.ListAsync();
            var orders = await _unitOfWork.OrdenesProduccion.ListAsync();
            var costHistory = await _unitOfWork.ProductCostHistory.ListAsync();
            var customers = await _unitOfWork.Usuarios.ListAsync();
            var latestBackup = (await _backupService.GetRecentBackupsAsync(1, cancellationToken)).FirstOrDefault();
            var monetaryState = await _monetaryConfigurationService.GetCurrentStateAsync(cancellationToken);
            var settings = await _backupService.GetSettingsAsync(cancellationToken);

            var databasePath = AppPaths.DatabaseFilePath;
            var databaseSizeBytes = File.Exists(databasePath) ? new FileInfo(databasePath).Length : 0;
            var backupEnabled = settings.Automatico || settings.GoogleDriveHabilitado || latestBackup is not null;
            var driveSyncEnabled = settings.GoogleDriveHabilitado && _cloudSyncService is not null;
            var backupStatus = latestBackup is null ? "Sin respaldo reciente" : "Activo";
            var cloudProvider = driveSyncEnabled ? "Google Drive" : "No configurado";
            var statusTone = latestBackup is null ? "Warning" : "Success";
            var criticalStockCount = resources.Count(resource => resource.StockActual <= resource.StockMinimo);
            var ordersInProcessCount = orders.Count(order => order.Estado == EstadoOrdenProduccion.EnProceso);
            var currentMonth = DateTime.UtcNow.Month;
            var currentYear = DateTime.UtcNow.Year;
            var monthlyCostTotal = costHistory
                .Where(history => history.Fecha.Month == currentMonth && history.Fecha.Year == currentYear)
                .Sum(history => history.CostoNuevo);

            return new SystemStatusSnapshot
            {
                ProductCount = products.Count(),
                ResourceCount = resources.Count(),
                CustomerCount = customers.Count(),
                CriticalStockCount = criticalStockCount,
                OrdersInProcessCount = ordersInProcessCount,
                MonthlyCostTotal = monthlyCostTotal,
                LastBackupAt = latestBackup?.FechaHora,
                LastDollarUpdateAt = monetaryState?.LastUpdate,
                LastDollarSource = monetaryState?.Source ?? string.Empty,
                DatabaseSizeBytes = databaseSizeBytes,
                IsInternetConnected = true,
                ApplicationVersion = "1.0.0",
                LastDriveSyncAt = latestBackup?.FechaHora,
                BackupEnabled = backupEnabled,
                DriveSyncEnabled = driveSyncEnabled,
                BackupStatus = backupStatus,
                CloudProvider = cloudProvider,
                StatusTone = statusTone
            };
        }
    }
}
