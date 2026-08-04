using System;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StockManufactura.Application.Interfaces;
using StockManufactura.Application.Services;
using StockManufactura.Desktop.Services;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Desktop.ViewModels
{
    public sealed class DashboardViewModel : ObservableObject
    {
        private readonly NavigationService _navigationService;
        private readonly IResourcePricingService _resourcePricingService;
        private readonly IMonetaryConfigurationService _monetaryConfigurationService;
        private readonly IAuditLogService _auditLogService;
        private readonly IBackupService _backupService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IProductCostService _productCostService;
        private readonly ISystemStatusService _systemStatusService;
        private SystemStatusSnapshot? _status;
        private string _statusMessage = string.Empty;

        public DashboardViewModel(
            Usuario usuario,
            NavigationService navigationService,
            IResourcePricingService resourcePricingService,
            IMonetaryConfigurationService monetaryConfigurationService,
            IAuditLogService auditLogService,
            IBackupService backupService,
            IUnitOfWork unitOfWork,
            IProductCostService productCostService,
            ISystemStatusService systemStatusService)
        {
            Usuario = usuario;
            _navigationService = navigationService;
            _resourcePricingService = resourcePricingService;
            _monetaryConfigurationService = monetaryConfigurationService;
            _auditLogService = auditLogService;
            _backupService = backupService;
            _unitOfWork = unitOfWork;
            _productCostService = productCostService;
            _systemStatusService = systemStatusService ?? throw new ArgumentNullException(nameof(systemStatusService));
            NavigateToResourcesCommand = new RelayCommand(NavigateToResources);
            NavigateToMonetaryConfigurationCommand = new RelayCommand(NavigateToMonetaryConfiguration);
            NavigateToAuditLogCommand = new RelayCommand(NavigateToAuditLog);
            NavigateToBackupsCommand = new RelayCommand(NavigateToBackups);
            NavigateToProductCostHistoryCommand = new RelayCommand(NavigateToProductCostHistory);
            RefreshStatusCommand = new AsyncRelayCommand(LoadStatusAsync);
            _ = LoadStatusAsync();
        }

        public Usuario Usuario { get; }

        public ICommand NavigateToResourcesCommand { get; }
        public ICommand NavigateToMonetaryConfigurationCommand { get; }
        public ICommand NavigateToAuditLogCommand { get; }
        public ICommand NavigateToBackupsCommand { get; }
        public ICommand NavigateToProductCostHistoryCommand { get; }
        public ICommand RefreshStatusCommand { get; }

        public SystemStatusSnapshot? Status
        {
            get => _status;
            private set => SetProperty(ref _status, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            private set => SetProperty(ref _statusMessage, value);
        }

        public string LastBackupText => Status?.LastBackupAt is DateTime backupAt ? backupAt.ToString("dd/MM/yyyy HH:mm") : "Sin respaldo";
        public string LastDriveSyncText => Status?.LastDriveSyncAt is DateTime syncAt ? syncAt.ToString("dd/MM/yyyy HH:mm") : "Sin sincronización";
        public string LastDollarUpdateText => Status?.LastDollarUpdateAt is DateTime dollarAt ? dollarAt.ToString("dd/MM/yyyy HH:mm") : "Sin cotización";
        public string InternetText => Status?.IsInternetConnected == true ? "Conectado" : "Sin conexión";
        public string DatabaseSizeText => FormatBytes(Status?.DatabaseSizeBytes ?? 0);
        public string ProductCountText => Status?.ProductCount.ToString() ?? "0";
        public string ResourceCountText => Status?.ResourceCount.ToString() ?? "0";
        public string CustomerCountText => Status?.CustomerCount.ToString() ?? "0";
        public string VersionText => Status?.ApplicationVersion ?? "1.0.0";
        public string DollarSourceText => Status?.LastDollarSource ?? "Sin fuente";
        public string BackupStatusText => Status?.BackupStatus ?? "Sin datos";
        public string CloudStatusText => Status?.DriveSyncEnabled == true ? $"{Status.CloudProvider}" : "Sin sincronización";
        public string StatusToneText => Status?.StatusTone ?? "Neutral";
        public bool BackupHealthy => Status?.BackupEnabled == true;
        public bool SyncHealthy => Status?.DriveSyncEnabled == true;

        private void NavigateToResources()
        {
            _navigationService.NavigateTo(new ResourceManagementViewModel(_resourcePricingService, _monetaryConfigurationService));
        }

        private void NavigateToMonetaryConfiguration()
        {
            _navigationService.NavigateTo(new MonetaryConfigurationViewModel(_monetaryConfigurationService));
        }

        private void NavigateToAuditLog()
        {
            _navigationService.NavigateTo(new AuditLogViewModel(_auditLogService));
        }

        private void NavigateToBackups()
        {
            _navigationService.NavigateTo(new BackupManagementViewModel(_backupService));
        }

        private void NavigateToProductCostHistory()
        {
            _navigationService.NavigateTo(new ProductCostHistoryViewModel(_unitOfWork, _productCostService));
        }

        private async Task LoadStatusAsync()
        {
            try
            {
                var snapshot = await _systemStatusService.GetSnapshotAsync();
                Status = snapshot;
                StatusMessage = string.Empty;
            }
            catch (Exception ex)
            {
                StatusMessage = $"No se pudo cargar el estado del sistema: {ex.Message}";
            }
        }

        private static string FormatBytes(long bytes)
        {
            const int scale = 1024;
            double value = bytes;
            string[] units = { "B", "KB", "MB", "GB" };
            int unitIndex = 0;

            while (value >= scale && unitIndex < units.Length - 1)
            {
                value /= scale;
                unitIndex++;
            }

            return unitIndex == 0 ? $"{value:F0} {units[unitIndex]}" : $"{value:F1} {units[unitIndex]}";
        }
    }
}
