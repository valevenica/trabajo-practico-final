using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using StockManufactura.Application.Interfaces;
using StockManufactura.Desktop.Infrastructure;
using StockManufactura.Desktop.Services;

namespace StockManufactura.Desktop.ViewModels
{
    public sealed class MainWindowViewModel : INotifyPropertyChanged
    {
        private readonly NavigationService _navigationService;
        private readonly IServiceProvider _serviceProvider;
        private object _currentViewModel = default!;
        private DashboardViewModel? _dashboardViewModel;
        private string _activeMenuKey = "Dashboard";

        public MainWindowViewModel(
            NavigationService navigationService,
            IServiceProvider serviceProvider)
        {
            _navigationService = navigationService;
            _serviceProvider = serviceProvider;

            LogoImageSource = DesktopAssetLoader.TryLoadLogoImage();

            NavigateDashboardCommand = new RelayCommand(NavigateDashboard);
            NavigateProductsCommand = new RelayCommand(NavigateProducts);
            NavigateNewProductCommand = new RelayCommand(NavigateNewProduct);
            NavigateBomCommand = new RelayCommand(NavigateBom);
            NavigateResourcesCommand = new RelayCommand(NavigateResources);
            NavigateNewResourceCommand = new RelayCommand(NavigateNewResource);
            NavigateProvidersCommand = new RelayCommand(NavigateProviders);
            NavigateProductionOrdersCommand = new RelayCommand(NavigateProductionOrders);
            NavigateMonetaryCommand = new RelayCommand(NavigateMonetary);
            NavigateAuditCommand = new RelayCommand(NavigateAudit);
            NavigateBackupsCommand = new RelayCommand(NavigateBackups);
            NavigateUsersCommand = new RelayCommand(NavigateUsers);
            NavigateCostsCommand = new RelayCommand(NavigateCosts);
        }

        public string WindowTitle => "Integra Manufacturing";

        public ImageSource? LogoImageSource { get; }

        public string CurrentUserName => AuthSession.Current?.Usuario?.Nombre ?? "Invitado";

        public bool IsShellVisible => CurrentViewModel is not LoginViewModel;

        public bool CanManageUsers => true;
        public bool CanViewProducts => true;
        public bool CanEditBom => true;
        public bool CanManageProviders => true;
        public bool CanManageProductionOrders => true;

        public bool IsDashboardSelected => _activeMenuKey == "Dashboard";
        public bool IsProductsSelected => _activeMenuKey == "Products";
        public bool IsNewProductSelected => _activeMenuKey == "NewProduct";
        public bool IsResourcesSelected => _activeMenuKey == "Resources";
        public bool IsNewResourceSelected => _activeMenuKey == "NewResource";
        public bool IsProvidersSelected => _activeMenuKey == "Providers";
        public bool IsProductionOrdersSelected => _activeMenuKey == "ProductionOrders";
        public bool IsBomSelected => _activeMenuKey == "Bom";
        public bool IsCostsSelected => _activeMenuKey == "Costs";
        public bool IsMonetarySelected => _activeMenuKey == "Monetary";
        public bool IsBackupsSelected => _activeMenuKey == "Backups";
        public bool IsUsersSelected => _activeMenuKey == "Users";
        public bool IsAuditSelected => _activeMenuKey == "Audit";

        public ICommand NavigateDashboardCommand { get; }
        public ICommand NavigateProductsCommand { get; }
        public ICommand NavigateNewProductCommand { get; }
        public ICommand NavigateBomCommand { get; }
        public ICommand NavigateResourcesCommand { get; }
        public ICommand NavigateNewResourceCommand { get; }
        public ICommand NavigateProvidersCommand { get; }
        public ICommand NavigateProductionOrdersCommand { get; }
        public ICommand NavigateMonetaryCommand { get; }
        public ICommand NavigateAuditCommand { get; }
        public ICommand NavigateBackupsCommand { get; }
        public ICommand NavigateUsersCommand { get; }
        public ICommand NavigateCostsCommand { get; }

        public object CurrentViewModel
        {
            get => _currentViewModel;
            set
            {
                if (Equals(_currentViewModel, value))
                {
                    return;
                }
                _currentViewModel = value;
                OnPropertyChanged();
                if (value is DashboardViewModel dashboard)
                {
                    _dashboardViewModel = dashboard;
                }

                OnPropertyChanged(nameof(IsShellVisible));
                OnPropertyChanged(nameof(CurrentUserName));
                OnPropertyChanged(nameof(CanManageUsers));
                OnPropertyChanged(nameof(CanViewProducts));
                OnPropertyChanged(nameof(CanEditBom));
                OnPropertyChanged(nameof(CanManageProviders));
                OnPropertyChanged(nameof(CanManageProductionOrders));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool EnsureDashboardAvailable(out DashboardViewModel dashboard)
        {
            if (_dashboardViewModel is not null)
            {
                dashboard = _dashboardViewModel;
                return true;
            }

            var user = AuthSession.Current?.Usuario;
            if (user is null)
            {
                dashboard = null!;
                return false;
            }

            _dashboardViewModel = new DashboardViewModel(
                user,
                _navigationService,
                _serviceProvider.GetRequiredService<IResourcePricingService>(),
                _serviceProvider.GetRequiredService<IMonetaryConfigurationService>(),
                _serviceProvider.GetRequiredService<IAuditLogService>(),
                _serviceProvider.GetRequiredService<IBackupService>(),
                _serviceProvider.GetRequiredService<IUnitOfWork>(),
                _serviceProvider.GetRequiredService<IProductCostService>(),
                _serviceProvider.GetRequiredService<ISystemStatusService>(),
                _serviceProvider.GetRequiredService<IUserManagementService>());

            dashboard = _dashboardViewModel;
            return true;
        }

        private static void ExecuteIfPossible(ICommand command)
        {
            if (command.CanExecute(null))
            {
                command.Execute(null);
            }
        }

        private void NavigateDashboard()
        {
            Log.Debug("NavigateDashboard called. AuthSession.Current?.Usuario = {usuario}", AuthSession.Current?.Usuario?.Nombre);
            if (!EnsureDashboardAvailable(out var dashboard))
            {
                Log.Warning("EnsureDashboardAvailable returned false");
                return;
            }

            _navigationService.NavigateTo(dashboard);
            SetActiveMenu("Dashboard");
            ExecuteIfPossible(dashboard.RefreshStatusCommand);
        }

        private void NavigateProducts()
        {
            Log.Debug("NavigateProducts called. AuthSession.Current?.Usuario = {usuario}", AuthSession.Current?.Usuario?.Nombre);
            if (EnsureDashboardAvailable(out var dashboard))
            {
                _navigationService.NavigateTo(new ProductManagementViewModel(
                    _serviceProvider.GetRequiredService<IUnitOfWork>(),
                    _serviceProvider.GetRequiredService<IAuditLogService>(),
                    _navigationService,
                    dashboard));
                SetActiveMenu("Products");
            }
            else
            {
                Log.Warning("EnsureDashboardAvailable returned false in NavigateProducts");
            }
        }

        private void NavigateNewProduct()
        {
            if (EnsureDashboardAvailable(out var dashboard))
            {
                _navigationService.NavigateTo(new ProductManagementViewModel(
                    _serviceProvider.GetRequiredService<IUnitOfWork>(),
                    _serviceProvider.GetRequiredService<IAuditLogService>(),
                    _navigationService,
                    dashboard,
                    startInCreateMode: true));
                SetActiveMenu("NewProduct");
            }
        }

        private void NavigateBom()
        {
            if (EnsureDashboardAvailable(out var dashboard))
            {
                _navigationService.NavigateTo(new BomManagementViewModel(
                    _serviceProvider.GetRequiredService<IUnitOfWork>(),
                    _serviceProvider.GetRequiredService<IAuditLogService>(),
                    _serviceProvider.GetRequiredService<IProductCostService>(),
                    _navigationService,
                    dashboard));
                SetActiveMenu("Bom");
            }
        }

        private void NavigateResources()
        {
            Log.Debug("NavigateResources called. AuthSession.Current?.Usuario = {usuario}", AuthSession.Current?.Usuario?.Nombre);
            if (EnsureDashboardAvailable(out _))
            {
                _navigationService.NavigateTo(new ResourceManagementViewModel(
                    _serviceProvider.GetRequiredService<IResourcePricingService>(),
                    _serviceProvider.GetRequiredService<IMonetaryConfigurationService>(),
                    _serviceProvider.GetRequiredService<IUnitOfWork>()));
                SetActiveMenu("Resources");
            }
            else
            {
                Log.Warning("EnsureDashboardAvailable returned false in NavigateResources");
            }
        }

        private void NavigateNewResource()
        {
            NavigateResources();
            SetActiveMenu("NewResource");
        }

        private void NavigateProviders()
        {
            if (EnsureDashboardAvailable(out var dashboard))
            {
                _navigationService.NavigateTo(new ProviderManagementViewModel(
                    _serviceProvider.GetRequiredService<IUnitOfWork>(),
                    _serviceProvider.GetRequiredService<IAuditLogService>(),
                    _navigationService,
                    dashboard));
                SetActiveMenu("Providers");
            }
        }

        private void NavigateProductionOrders()
        {
            if (EnsureDashboardAvailable(out var dashboard))
            {
                _navigationService.NavigateTo(new ProductionOrderManagementViewModel(
                    _serviceProvider.GetRequiredService<IUnitOfWork>(),
                    _serviceProvider.GetRequiredService<IAuditLogService>(),
                    _navigationService,
                    dashboard));
                SetActiveMenu("ProductionOrders");
            }
        }

        private void NavigateMonetary()
        {
            if (EnsureDashboardAvailable(out _))
            {
                _navigationService.NavigateTo(new MonetaryConfigurationViewModel(
                    _serviceProvider.GetRequiredService<IMonetaryConfigurationService>()));
                SetActiveMenu("Monetary");
            }
        }

        private void NavigateAudit()
        {
            if (EnsureDashboardAvailable(out _))
            {
                _navigationService.NavigateTo(new AuditLogViewModel(
                    _serviceProvider.GetRequiredService<IAuditLogService>()));
                SetActiveMenu("Audit");
            }
        }

        private void NavigateBackups()
        {
            if (EnsureDashboardAvailable(out _))
            {
                _navigationService.NavigateTo(new BackupManagementViewModel(
                    _serviceProvider.GetRequiredService<IBackupService>()));
                SetActiveMenu("Backups");
            }
        }

        private void NavigateUsers()
        {
            if (EnsureDashboardAvailable(out var dashboard))
            {
                _navigationService.NavigateTo(new UserManagementViewModel(
                    _serviceProvider.GetRequiredService<IUserManagementService>(),
                    _navigationService,
                    dashboard));
                SetActiveMenu("Users");
            }
        }

        private void NavigateCosts()
        {
            if (EnsureDashboardAvailable(out var dashboard))
            {
                _navigationService.NavigateTo(new ProductCostHistoryViewModel(
                    _serviceProvider.GetRequiredService<IUnitOfWork>(),
                    _serviceProvider.GetRequiredService<IProductCostService>(),
                    _navigationService,
                    dashboard));
                SetActiveMenu("Costs");
            }
        }

        private void SetActiveMenu(string key)
        {
            if (_activeMenuKey == key)
            {
                return;
            }

            _activeMenuKey = key;
            OnPropertyChanged(nameof(IsDashboardSelected));
            OnPropertyChanged(nameof(IsProductsSelected));
            OnPropertyChanged(nameof(IsNewProductSelected));
            OnPropertyChanged(nameof(IsResourcesSelected));
            OnPropertyChanged(nameof(IsNewResourceSelected));
            OnPropertyChanged(nameof(IsProvidersSelected));
            OnPropertyChanged(nameof(IsProductionOrdersSelected));
            OnPropertyChanged(nameof(IsBomSelected));
            OnPropertyChanged(nameof(IsCostsSelected));
            OnPropertyChanged(nameof(IsMonetarySelected));
            OnPropertyChanged(nameof(IsBackupsSelected));
            OnPropertyChanged(nameof(IsUsersSelected));
            OnPropertyChanged(nameof(IsAuditSelected));
        }
    }
}
