using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StockManufactura.Application.Interfaces;
using StockManufactura.Application.Services;
using StockManufactura.Desktop.Infrastructure;
using StockManufactura.Desktop.Services;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Desktop.ViewModels
{
    public sealed class StatusFilterItem : INotifyPropertyChanged
    {
        private bool _isChecked;
        public string Label { get; }
        public string Value { get; }

        public StatusFilterItem(string label, string value, bool isChecked = true)
        {
            Label = label;
            Value = value;
            _isChecked = isChecked;
        }

        public bool IsChecked
        {
            get => _isChecked;
            set { _isChecked = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsChecked))); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    public sealed class PeriodOption
    {
        public int? Year { get; }
        public int? Month { get; }
        public string Label { get; }

        public PeriodOption(int? year, int? month, string label)
        {
            Year = year;
            Month = month;
            Label = label;
        }

        public override string ToString() => Label;
    }

    public sealed partial class DashboardViewModel : ObservableObject
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
        private readonly IShortageService _shortageService;
        private readonly IPasswordRecoveryService _passwordRecoveryService;
        private SystemStatusSnapshot? _status;
        private string _statusMessage = string.Empty;
        private readonly ICollectionView _ordersView;

        // Las pantallas que se abren desde el panel reciben su propio contexto de datos: el del panel vive toda la sesión
        // y compartirlo hacía que las consultas de ambas pantallas se pisaran.
        private readonly Func<IUnitOfWork>? _unitOfWorkFactory;

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
            IUserManagementService userManagementService,
            IShortageService shortageService,
            IPasswordRecoveryService passwordRecoveryService,
            Func<IUnitOfWork>? unitOfWorkFactory = null)
        {
            _unitOfWorkFactory = unitOfWorkFactory;
            _passwordRecoveryService = passwordRecoveryService;
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
            _shortageService = shortageService ?? throw new ArgumentNullException(nameof(shortageService));
            NavigateToShortagesCommand = new RelayCommand(NavigateToShortages);
            NavigateToResourcesCommand = new RelayCommand(NavigateToResources);
            NavigateToMonetaryConfigurationCommand = new RelayCommand(NavigateToMonetaryConfiguration);
            NavigateToAuditLogCommand = new RelayCommand(NavigateToAuditLog);
            NavigateToBackupsCommand = new RelayCommand(NavigateToBackups);
            NavigateToProductCostHistoryCommand = new RelayCommand(NavigateToProductCostHistory);
            NavigateToProductsCommand = new RelayCommand(NavigateToProducts);
            NavigateToProvidersCommand = new RelayCommand(NavigateToProviders);
            NavigateToProductionOrdersCommand = new RelayCommand(NavigateToProductionOrders);
            NavigateToUserManagementCommand = new RelayCommand(NavigateToUserManagement);
            NavigateToMyAccountCommand = new RelayCommand(NavigateToMyAccount);
            RefreshStatusCommand = new AsyncRelayCommand(LoadStatusAsync);
            OrderStatusFilters = new ObservableCollection<StatusFilterItem>
            {
                new StatusFilterItem("En espera",  "En espera"),
                new StatusFilterItem("En proceso", "En proceso"),
                new StatusFilterItem("Finalizada", "Finalizada"),
                new StatusFilterItem("Cancelada",  "Cancelada", isChecked: false)
            };
            foreach (var f in OrderStatusFilters)
            {
                f.PropertyChanged += (_, _) =>
                {
                    _ordersView.Refresh();
                    OnPropertyChanged(nameof(SelectAllStatusFilters));
                    OnPropertyChanged(nameof(FilterSummary));
                };
            }
            OrderSortOptions = new ObservableCollection<string>(new[] { "Más recientes", "Más antiguas", "ID", "Estado", "Costo mayor", "Costo menor" });
            Orders = new ObservableCollection<DashboardOrderRow>();
            _ordersView = CollectionViewSource.GetDefaultView(Orders);
            _ordersView.Filter = FilterOrder;
            ApplyOrderSorting();

            SelectedOrderStatusFilter = "Todas";
            SelectedOrderSortOption = "Más recientes";
            PeriodOptions = new ObservableCollection<PeriodOption>(BuildPeriodOptions());
            SelectedPeriod = PeriodOptions.FirstOrDefault(p => p.Year == DateTime.UtcNow.Year && p.Month == DateTime.UtcNow.Month)
                ?? PeriodOptions[0];
            AdvanceOrderCommand = new AsyncRelayCommand<DashboardOrderRow>(AdvanceOrderAsync);
            CancelOrderCommand = new AsyncRelayCommand<DashboardOrderRow>(CancelOrderAsync);
            _ = LoadStatusAsync();
        }

        public Usuario Usuario { get; }

        public ICommand NavigateToShortagesCommand { get; }
        public ICommand NavigateToResourcesCommand { get; }
        public ICommand NavigateToMonetaryConfigurationCommand { get; }
        public ICommand NavigateToAuditLogCommand { get; }
        public ICommand NavigateToBackupsCommand { get; }
        public ICommand NavigateToProductCostHistoryCommand { get; }
        public ICommand NavigateToProductsCommand { get; }
        public ICommand NavigateToProvidersCommand { get; }
        public ICommand NavigateToProductionOrdersCommand { get; }
        public ICommand NavigateToUserManagementCommand { get; }
        public ICommand NavigateToMyAccountCommand { get; }

        /// <summary>Contraseña inicial o temporal todavía no reemplazada: el panel muestra el aviso.</summary>
        public bool IsPasswordChangePending => ModuleAccess.PasswordChangePending;

        /// <summary>Lo invoca "Mi cuenta" al completar el cambio, para ocultar el aviso.</summary>
        public void NotifyPasswordChanged() => OnPropertyChanged(nameof(IsPasswordChangePending));
        public ICommand RefreshStatusCommand { get; }
        public ICommand AdvanceOrderCommand { get; }
        public ICommand CancelOrderCommand { get; }
        public ObservableCollection<DashboardOrderRow> Orders { get; }
        public ObservableCollection<StatusFilterItem> OrderStatusFilters { get; }
        public ObservableCollection<string> OrderSortOptions { get; }
        public ObservableCollection<PeriodOption> PeriodOptions { get; }

        public ICollectionView OrdersView => _ordersView;

        public bool SelectAllStatusFilters
        {
            get => OrderStatusFilters.All(f => f.IsChecked);
            set
            {
                foreach (var f in OrderStatusFilters) f.IsChecked = value;
                _ordersView.Refresh();
                OnPropertyChanged();
            }
        }

        public string FilterSummary
        {
            get
            {
                var active = OrderStatusFilters.Where(f => f.IsChecked).ToList();
                if (active.Count == 0) return "Sin filtro";
                if (active.Count == OrderStatusFilters.Count) return "Todos los estados";
                return string.Join(", ", active.Select(f => f.Label));
            }
        }

        [ObservableProperty] private string _selectedOrderStatusFilter = string.Empty;
        [ObservableProperty] private string _selectedOrderSortOption = string.Empty;
        [ObservableProperty] private string _orderIdSearchText = string.Empty;
        [ObservableProperty] private PeriodOption? _selectedPeriod;

        public bool CanManageUsers => AuthSession.Current?.TienePermiso("USUARIOS_ADMIN") == true;
        public bool CanViewProducts => AuthSession.Current?.TienePermiso("PRODUCTOS_VER") == true
            || AuthSession.Current?.TienePermiso("PRODUCTOS_CREAR") == true
            || AuthSession.Current?.TienePermiso("PRODUCTOS_EDITAR") == true;
        public bool CanManageProviders => AuthSession.Current?.TienePermiso("USUARIOS_ADMIN") == true
            || AuthSession.Current?.TienePermiso("PRODUCTOS_EDITAR") == true;
        public bool CanManageProductionOrders => AuthSession.Current?.TienePermiso("PRODUCTOS_EDITAR") == true;

        public SystemStatusSnapshot? Status
        {
            get => _status;
            private set
            {
                if (!SetProperty(ref _status, value))
                {
                    return;
                }

                // Las tarjetas del panel se apoyan en propiedades derivadas del snapshot:
                // sin estas notificaciones quedarían mostrando el valor de la carga anterior.
                foreach (var derived in StatusDerivedProperties)
                {
                    OnPropertyChanged(derived);
                }
            }
        }

        private static readonly string[] StatusDerivedProperties =
        {
            nameof(LastBackupText), nameof(LastDriveSyncText), nameof(LastDollarUpdateText),
            nameof(InternetText), nameof(DatabaseSizeText), nameof(ProductCountText),
            nameof(ResourceCountText), nameof(CustomerCountText), nameof(VersionText),
            nameof(DollarSourceText), nameof(BackupStatusText), nameof(CloudStatusText),
            nameof(StatusToneText), nameof(BackupHealthy), nameof(SyncHealthy),
            nameof(SalesTotalText), nameof(SalesVariationText), nameof(SalesVariationPositive), nameof(SalesVariationNegative),
            nameof(OrdersInPeriodText), nameof(OpenOrdersText), nameof(AverageTicketText), nameof(AverageTicketDetailText),
            nameof(GrossMarginText), nameof(GrossMarginDetailText), nameof(CriticalStockCount), nameof(HasCriticalStock)
        };

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

        // Indicadores comerciales del período seleccionado. Los indicadores de trazabilidad y auditoría
        // se muestran en la pantalla de Auditoría, reservada al administrador.
        private static readonly System.Globalization.CultureInfo Moneda = System.Globalization.CultureInfo.GetCultureInfo("es-AR");

        private static string Pesos(decimal value) => $"$ {value.ToString("N2", Moneda)}";

        public string SalesTotalText => Pesos(Status?.MonthlySalesTotal ?? 0m);
        public string SalesVariationText => Status?.SalesVariationPercent is decimal variation
            ? $"{(variation >= 0 ? "▲" : "▼")} {Math.Abs(variation).ToString("0.#", Moneda)} % vs. mes anterior"
            : Status?.PreviousPeriodSalesTotal is null ? "Sin comparación para el período" : "Sin ventas el mes anterior";
        public bool SalesVariationPositive => Status?.SalesVariationPercent >= 0;
        public bool SalesVariationNegative => Status?.SalesVariationPercent < 0;
        public string OrdersInPeriodText => (Status?.OrdersInPeriodCount ?? 0).ToString();
        public string OpenOrdersText => $"{Status?.OpenOrdersCount ?? 0} en curso · {Status?.FinishedOrdersInPeriodCount ?? 0} finalizados";
        public string AverageTicketText => Pesos(Status?.AverageTicket ?? 0m);
        public string AverageTicketDetailText => "Venta promedio por pedido finalizado";
        public string GrossMarginText => Status?.GrossMarginPercent is decimal margin
            ? $"{margin.ToString("0.#", Moneda)} %"
            : "—";
        public string GrossMarginDetailText => Status is null || Status.MonthlySalesTotal == 0
            ? "Sin ventas en el período"
            : $"Ganancia bruta {Pesos(Status.MonthlySalesTotal - Status.MonthlySalesCost)}";
        public int CriticalStockCount => Status?.CriticalStockCount ?? 0;
        public bool HasCriticalStock => CriticalStockCount > 0;

        partial void OnSelectedOrderStatusFilterChanged(string value)
        {
            _ordersView.Refresh();
        }

        partial void OnSelectedOrderSortOptionChanged(string value)
        {
            ApplyOrderSorting();
            _ordersView.Refresh();
        }

        partial void OnOrderIdSearchTextChanged(string value)
        {
            _ordersView.Refresh();
        }

        partial void OnSelectedPeriodChanged(PeriodOption? value)
        {
            _ = LoadStatusAsync();
        }

        private static IEnumerable<PeriodOption> BuildPeriodOptions()
        {
            yield return new PeriodOption(null, null, "Todos los períodos");

            var culture = CultureInfo.GetCultureInfo("es-AR");
            var cursor = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            for (var i = 0; i < 12; i++)
            {
                var label = cursor.ToString("MMMM yyyy", culture);
                label = char.ToUpperInvariant(label[0]) + label[1..];
                yield return new PeriodOption(cursor.Year, cursor.Month, label);
                cursor = cursor.AddMonths(-1);
            }
        }

        private void NavigateToShortages()
        {
            if (!ModuleAccess.TryEnter(ModuleAccess.Shortages))
            {
                return;
            }

            _navigationService.NavigateTo(new ShortagesViewModel(_shortageService, _navigationService, this));
        }

        private void NavigateToResources()
        {
            if (!ModuleAccess.TryEnter(ModuleAccess.Resources))
            {
                return;
            }

            _navigationService.NavigateTo(new ResourceManagementViewModel(_resourcePricingService, _monetaryConfigurationService, _unitOfWork, _auditLogService));
        }

        private void NavigateToMonetaryConfiguration()
        {
            if (!ModuleAccess.TryEnter(ModuleAccess.Monetary))
            {
                return;
            }

            _navigationService.NavigateTo(new MonetaryConfigurationViewModel(_monetaryConfigurationService));
        }

        private void NavigateToAuditLog()
        {
            if (!ModuleAccess.TryEnter(ModuleAccess.Audit))
            {
                return;
            }

            _navigationService.NavigateTo(new AuditLogViewModel(_auditLogService, _systemStatusService));
        }

        private void NavigateToBackups()
        {
            if (!ModuleAccess.TryEnter(ModuleAccess.Backups))
            {
                return;
            }

            _navigationService.NavigateTo(new BackupManagementViewModel(_backupService));
        }

        private void NavigateToProductCostHistory()
        {
            if (!ModuleAccess.TryEnter(ModuleAccess.CostHistory))
            {
                return;
            }

            _navigationService.NavigateTo(new ProductCostHistoryViewModel(_unitOfWork, _productCostService, _navigationService, this));
        }

        private void NavigateToProducts()
        {
            if (!ModuleAccess.TryEnter(ModuleAccess.Products))
            {
                return;
            }

            if (!CanViewProducts)
            {
                StatusMessage = "No tiene permisos para gestionar productos.";
                return;
            }

            _navigationService.NavigateTo(new ProductManagementViewModel(_unitOfWork, _auditLogService, _productCostService, _navigationService, this));
        }

        private void NavigateToProviders()
        {
            if (!ModuleAccess.TryEnter(ModuleAccess.Providers))
            {
                return;
            }

            if (!CanManageProviders)
            {
                StatusMessage = "No tiene permisos para gestionar proveedores.";
                return;
            }

            _navigationService.NavigateTo(new ProviderManagementViewModel(_unitOfWork, _auditLogService, _navigationService, this));
        }

        private void NavigateToProductionOrders()
        {
            if (!ModuleAccess.TryEnter(ModuleAccess.ProductionOrders))
            {
                return;
            }

            if (!CanManageProductionOrders)
            {
                StatusMessage = "No tiene permisos para gestionar órdenes de producción.";
                return;
            }

            _navigationService.NavigateTo(new ProductionOrderManagementViewModel(
                _unitOfWorkFactory?.Invoke() ?? _unitOfWork, _auditLogService, _navigationService, this, _shortageService));
        }

        private void NavigateToMyAccount()
        {
            _navigationService.NavigateTo(new UserManagementViewModel(_userManagementService, _passwordRecoveryService, _navigationService, this));
        }

        private void NavigateToUserManagement()
        {
            if (!CanManageUsers)
            {
                StatusMessage = "No tiene permisos para administrar usuarios.";
                return;
            }

            _navigationService.NavigateTo(new UserManagementViewModel(_userManagementService, _passwordRecoveryService, _navigationService, this));
        }

        // El panel comparte un contexto de datos entre todas sus cargas, y al abrirse se disparan dos a la vez (la del
        // período inicial y la explícita): se serializan para que no usen el contexto en paralelo.
        private readonly SemaphoreSlim _loadGate = new(1, 1);

        private async Task LoadStatusAsync()
        {
            await _loadGate.WaitAsync();
            try
            {
                try
                {
                    var period = SelectedPeriod;
                    var (snapshot, orders) = await Task.Run(async () =>
                    {
                        var s = await _systemStatusService.GetSnapshotAsync(period?.Year, period?.Month);
                        var o = await _unitOfWork.OrdenesProduccion.ListByCreatedDescAsync();
                        return (s, o);
                    });

                    var productIds = orders.Select(x => x.ProductoId).Distinct();
                    var products = await Task.Run(async () => await _unitOfWork.Productos.ListByIdsAsync(productIds.ToArray()));
                    var clients = await Task.Run(async () => await _unitOfWork.Clientes.ListAsync());

                    Status = snapshot;
                    LoadOrders(orders, products, clients);
                    StatusMessage = string.Empty;
                }
                catch (Exception ex)
                {
                    StatusMessage = $"No se pudo cargar el estado del sistema: {ex.Message}";
                }
            }
            finally
            {
                _loadGate.Release();
            }
        }

        private void LoadOrders(IEnumerable<OrdenProduccion> orders, IEnumerable<Producto> products, IEnumerable<Cliente> clients)
        {
            var productsById = products.ToDictionary(product => product.Id, product => product);
            var clientNames = clients.ToDictionary(client => client.Id, client => client.Nombre);
            Orders.Clear();

            foreach (var order in orders)
            {
                productsById.TryGetValue(order.ProductoId, out var product);
                var productName = product?.Nombre ?? "Producto eliminado";
                var estimatedCost = (product?.CostoFabricacionActual ?? 0m) * order.CantidadPlaneada;
                var clientName = order.ClienteId is Guid clientId && clientNames.TryGetValue(clientId, out var name)
                    ? name
                    : ClientOption.ConsumidorFinal;
                Orders.Add(new DashboardOrderRow(order, productName, estimatedCost, clientName));
            }

            OnPropertyChanged(nameof(Orders));
            _ordersView.Refresh();
        }

        private bool FilterOrder(object obj)
        {
            if (obj is not DashboardOrderRow row) return false;

            // Los pedidos abiertos son trabajo pendiente y se muestran en cualquier período, aunque se hayan creado
            // en un mes anterior; el período solo acota los pedidos ya finalizados o cancelados.
            if (!row.IsOpen
                && SelectedPeriod is { Year: not null, Month: not null } period
                && (row.PeriodDate.Year != period.Year || row.PeriodDate.Month != period.Month))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(OrderIdSearchText)
                && row.Codigo.IndexOf(OrderIdSearchText, StringComparison.OrdinalIgnoreCase) < 0
                && row.ClientName.IndexOf(OrderIdSearchText, StringComparison.OrdinalIgnoreCase) < 0)
            {
                return false;
            }

            var activeFilters = OrderStatusFilters.Where(f => f.IsChecked).Select(f => f.Value).ToHashSet();
            return activeFilters.Count == 0 || activeFilters.Contains(row.StatusFilter);
        }

        private async Task AdvanceOrderAsync(DashboardOrderRow? row)
        {
            if (row is null) return;
            if (!ModuleAccess.TryEnter(ModuleAccess.ProductionOrders)) return;
            try
            {
                var order = await _unitOfWork.OrdenesProduccion.GetByIdAsync(row.OrderId);
                if (order is null) return;

                if (order.Estado is EstadoOrdenProduccion.EnProceso)
                    order.Completar();
                else
                    order.MarcarEnProgreso();

                _unitOfWork.OrdenesProduccion.Update(order);
                await _unitOfWork.SaveChangesAsync();
                await LoadStatusAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error al cambiar estado: {ex.Message}";
            }
        }

        private async Task CancelOrderAsync(DashboardOrderRow? row)
        {
            if (row is null) return;
            if (!ModuleAccess.TryEnter(ModuleAccess.ProductionOrders)) return;
            try
            {
                var order = await _unitOfWork.OrdenesProduccion.GetByIdAsync(row.OrderId);
                if (order is null) return;

                order.Cancelar("Cancelada desde panel principal");
                _unitOfWork.OrdenesProduccion.Update(order);
                await _unitOfWork.SaveChangesAsync();
                await LoadStatusAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error al cancelar: {ex.Message}";
            }
        }

        private void ApplyOrderSorting()
        {
            _ordersView.SortDescriptions.Clear();

            switch (SelectedOrderSortOption)
            {
                case "Más antiguas":
                    _ordersView.SortDescriptions.Add(new SortDescription(nameof(DashboardOrderRow.CreatedAt), ListSortDirection.Ascending));
                    break;
                case "ID":
                    _ordersView.SortDescriptions.Add(new SortDescription(nameof(DashboardOrderRow.Codigo), ListSortDirection.Ascending));
                    break;
                case "Estado":
                    _ordersView.SortDescriptions.Add(new SortDescription(nameof(DashboardOrderRow.StatusSortOrder), ListSortDirection.Ascending));
                    _ordersView.SortDescriptions.Add(new SortDescription(nameof(DashboardOrderRow.CreatedAt), ListSortDirection.Descending));
                    break;
                case "Costo mayor":
                    _ordersView.SortDescriptions.Add(new SortDescription(nameof(DashboardOrderRow.EstimatedCostValue), ListSortDirection.Descending));
                    break;
                case "Costo menor":
                    _ordersView.SortDescriptions.Add(new SortDescription(nameof(DashboardOrderRow.EstimatedCostValue), ListSortDirection.Ascending));
                    break;
                case "Más recientes":
                default:
                    _ordersView.SortDescriptions.Add(new SortDescription(nameof(DashboardOrderRow.CreatedAt), ListSortDirection.Descending));
                    break;
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
