using System;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StockManufactura.Application.Interfaces;
using StockManufactura.Application.Services;
using StockManufactura.Desktop.Infrastructure;
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
        private readonly IUserManagementService _userManagementService;
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
            ISystemStatusService systemStatusService,
            IUserManagementService userManagementService)
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
            _userManagementService = userManagementService ?? throw new ArgumentNullException(nameof(userManagementService));
            NavigateToResourcesCommand = new RelayCommand(NavigateToResources);
            NavigateToMonetaryConfigurationCommand = new RelayCommand(NavigateToMonetaryConfiguration);
            NavigateToAuditLogCommand = new RelayCommand(NavigateToAuditLog);
            NavigateToBackupsCommand = new RelayCommand(NavigateToBackups);
            NavigateToProductCostHistoryCommand = new RelayCommand(NavigateToProductCostHistory);
            NavigateToProductsCommand = new RelayCommand(NavigateToProducts);
            NavigateToBomCommand = new RelayCommand(NavigateToBom);
            NavigateToProvidersCommand = new RelayCommand(NavigateToProviders);
            NavigateToProductionOrdersCommand = new RelayCommand(NavigateToProductionOrders);
            NavigateToUserManagementCommand = new RelayCommand(NavigateToUserManagement);
            RefreshStatusCommand = new AsyncRelayCommand(LoadStatusAsync);
            _ = LoadStatusAsync();
        }

        public Usuario Usuario { get; }

        public ICommand NavigateToResourcesCommand { get; }
        public ICommand NavigateToMonetaryConfigurationCommand { get; }
        public ICommand NavigateToAuditLogCommand { get; }
        public ICommand NavigateToBackupsCommand { get; }
        public ICommand NavigateToProductCostHistoryCommand { get; }
        public ICommand NavigateToProductsCommand { get; }
        public ICommand NavigateToBomCommand { get; }
        public ICommand NavigateToProvidersCommand { get; }
        public ICommand NavigateToProductionOrdersCommand { get; }
        public ICommand NavigateToUserManagementCommand { get; }
        public ICommand RefreshStatusCommand { get; }
        public bool CanManageUsers => AuthSession.Current?.TienePermiso("USUARIOS_ADMIN") == true;
        public bool CanViewProducts => AuthSession.Current?.TienePermiso("PRODUCTOS_VER") == true
            || AuthSession.Current?.TienePermiso("PRODUCTOS_CREAR") == true
            || AuthSession.Current?.TienePermiso("PRODUCTOS_EDITAR") == true;
        public bool CanEditBom => AuthSession.Current?.TienePermiso("PRODUCTOS_EDITAR") == true;
        public bool CanManageProviders => AuthSession.Current?.TienePermiso("USUARIOS_ADMIN") == true
            || AuthSession.Current?.TienePermiso("PRODUCTOS_EDITAR") == true;
        public bool CanManageProductionOrders => AuthSession.Current?.TienePermiso("PRODUCTOS_EDITAR") == true;

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

        private void NavigateToProducts()
        {
            if (!CanViewProducts)
            {
                StatusMessage = "No tiene permisos para gestionar productos.";
                return;
            }

            _navigationService.NavigateTo(new ProductManagementViewModel(_unitOfWork, _auditLogService, _navigationService, this));
        }

        private void NavigateToBom()
        {
            if (!CanEditBom)
            {
                StatusMessage = "No tiene permisos para editar recetas BOM.";
                return;
            }

            _navigationService.NavigateTo(new BomManagementViewModel(_unitOfWork, _auditLogService, _productCostService, _navigationService, this));
        }

        private void NavigateToProviders()
        {
            if (!CanManageProviders)
            {
                StatusMessage = "No tiene permisos para gestionar proveedores.";
                return;
            }

            _navigationService.NavigateTo(new ProviderManagementViewModel(_unitOfWork, _auditLogService, _navigationService, this));
        }

        private void NavigateToProductionOrders()
        {
            if (!CanManageProductionOrders)
            {
                StatusMessage = "No tiene permisos para gestionar órdenes de producción.";
                return;
            }

            _navigationService.NavigateTo(new ProductionOrderManagementViewModel(_unitOfWork, _auditLogService, _navigationService, this));
        }

        private void NavigateToUserManagement()
        {
            if (!CanManageUsers)
            {
                StatusMessage = "No tiene permisos para administrar usuarios.";
                return;
            }

            _navigationService.NavigateTo(new UserManagementViewModel(_userManagementService, _navigationService, this));
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
