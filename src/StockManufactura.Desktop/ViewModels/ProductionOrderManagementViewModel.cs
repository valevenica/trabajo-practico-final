using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StockManufactura.Application.Interfaces;
using StockManufactura.Desktop.Infrastructure;
using StockManufactura.Desktop.Services;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Desktop.ViewModels
{
    public sealed partial class ProductionOrderManagementViewModel : ObservableObject
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditLogService _auditLogService;
        private readonly NavigationService _navigationService;
        private readonly DashboardViewModel _dashboardViewModel;

        [ObservableProperty] private Producto? _selectedProduct;
        [ObservableProperty] private OrdenProduccion? _selectedOrder;
        [ObservableProperty] private string _cantidadPlaneada = "0";
        [ObservableProperty] private string _cantidadProducida = "0";
        [ObservableProperty] private string _observaciones = string.Empty;
        [ObservableProperty] private string _statusMessage = string.Empty;
        [ObservableProperty] private string _codigoInput = string.Empty;

        public ProductionOrderManagementViewModel(
            IUnitOfWork unitOfWork,
            IAuditLogService auditLogService,
            NavigationService navigationService,
            DashboardViewModel dashboardViewModel)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _dashboardViewModel = dashboardViewModel ?? throw new ArgumentNullException(nameof(dashboardViewModel));

            Products = new ObservableCollection<Producto>();
            Orders = new ObservableCollection<OrdenProduccion>();

            LoadCommand = new AsyncRelayCommand(LoadAsync);
            CreateOrderCommand = new AsyncRelayCommand(CreateOrderAsync);
            SetDraftCommand = new AsyncRelayCommand(SetDraftAsync);
            PlanifyCommand = new AsyncRelayCommand(PlanifyAsync);
            StartCommand = new AsyncRelayCommand(StartAsync);
            RegisterProductionCommand = new AsyncRelayCommand(RegisterProductionAsync);
            CompleteCommand = new AsyncRelayCommand(CompleteAsync);
            CancelCommand = new AsyncRelayCommand(CancelAsync);
            BackCommand = new RelayCommand(GoBack);

            _ = LoadAsync();
        }

        public ObservableCollection<Producto> Products { get; }
        public ObservableCollection<OrdenProduccion> Orders { get; }

        public ICommand LoadCommand { get; }
        public ICommand CreateOrderCommand { get; }
        public ICommand SetDraftCommand { get; }
        public ICommand PlanifyCommand { get; }
        public ICommand StartCommand { get; }
        public ICommand RegisterProductionCommand { get; }
        public ICommand CompleteCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand BackCommand { get; }

        public bool CanManageOrders => AuthSession.Current?.TienePermiso("PRODUCTOS_EDITAR") == true;
        public string CurrentUserName => AuthSession.Current?.Usuario?.Nombre ?? "Administrador";
        public string FactoryConnectionStatus => "CONECTADO";
        public int CriticalStockCount => 7;
        public int OrdersInProcessCount => Orders.Count(x => x.Estado == EstadoOrdenProduccion.EnProceso);
        public int OrdersCompletedCount => Orders.Count(x => x.Estado == EstadoOrdenProduccion.Finalizada);
        public decimal EstimatedOrdersCost => Orders.Sum(order =>
        {
            var product = Products.FirstOrDefault(x => x.Id == order.ProductoId);
            return (product?.CostoFabricacionActual ?? 0m) * order.CantidadPlaneada;
        });

        public string EstadoActual => SelectedOrder?.Estado.ToString() ?? "Sin selección";

        partial void OnSelectedOrderChanged(OrdenProduccion? value)
        {
            if (value is null)
            {
                return;
            }

            SelectedProduct = Products.FirstOrDefault(x => x.Id == value.ProductoId);
            CantidadPlaneada = value.CantidadPlaneada.ToString("0.0000", CultureInfo.InvariantCulture);
            CantidadProducida = value.CantidadProducida.ToString("0.0000", CultureInfo.InvariantCulture);
            Observaciones = value.Observaciones;
            OnPropertyChanged(nameof(EstadoActual));
        }

        private async Task LoadAsync()
        {
            try
            {
                var (products, orders) = await Task.Run(async () =>
                {
                    var p = await _unitOfWork.Productos.ListAsync();
                    var o = await _unitOfWork.OrdenesProduccion.ListByCreatedDescAsync();
                    return (p, o);
                });

                Products.Clear();
                foreach (var product in products.Where(x => x.Activo).OrderBy(x => x.Nombre))
                    Products.Add(product);

                Orders.Clear();
                foreach (var order in orders)
                    Orders.Add(order);

                if (SelectedProduct is null && Products.Count > 0)
                    SelectedProduct = Products[0];

                RefreshIndicators();
                StatusMessage = "Órdenes cargadas.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error al cargar órdenes: {ex.Message}";
            }
        }

        private async Task CreateOrderAsync()
        {
            if (!CanManageOrders)
            {
                StatusMessage = "No tiene permisos para gestionar órdenes.";
                return;
            }

            if (SelectedProduct is null)
            {
                StatusMessage = "Debe seleccionar un producto.";
                return;
            }

            if (!decimal.TryParse(CantidadPlaneada, NumberStyles.Number, CultureInfo.InvariantCulture, out var planned) || planned <= 0)
            {
                StatusMessage = "Cantidad planeada inválida.";
                return;
            }

            var codigo = string.IsNullOrWhiteSpace(CodigoInput) ? GenerateCode() : CodigoInput.Trim();
            var order = new OrdenProduccion(codigo, SelectedProduct.Id, planned, Observaciones.Trim());
            await _unitOfWork.OrdenesProduccion.AddAsync(order);
            await _unitOfWork.SaveChangesAsync();
            await RegisterAuditAsync("Crear", order, "Alta de orden de producción.");

            CodigoInput = codigo; // keep code visible so user can reuse it for next product
            await LoadAsync();
            SelectedOrder = Orders.FirstOrDefault(x => x.Id == order.Id);
            RefreshIndicators();
            StatusMessage = $"Orden {codigo} creada. Para agregar otro producto a la misma orden, mantené el código y seleccioná otro producto.";
        }

        private async Task SetDraftAsync()
        {
            if (!TryEnsureSelectedOrder(out var order))
            {
                return;
            }

            await TryTransitionAsync(() => order.VolverABorrador(), "Borrador", order, "Orden regresada a borrador.");
        }

        private async Task PlanifyAsync()
        {
            if (!TryEnsureSelectedOrder(out var order))
            {
                return;
            }

            await TryTransitionAsync(() => order.Planificar(), "Planificada", order, "Orden planificada.");
        }

        private async Task StartAsync()
        {
            if (!TryEnsureSelectedOrder(out var order))
            {
                return;
            }

            await TryTransitionAsync(() => order.MarcarEnProgreso(), "EnProceso", order, "Orden en proceso.");
        }

        private async Task RegisterProductionAsync()
        {
            if (!TryEnsureSelectedOrder(out var order))
            {
                return;
            }

            if (!decimal.TryParse(CantidadProducida, NumberStyles.Number, CultureInfo.InvariantCulture, out var produced) || produced < 0)
            {
                StatusMessage = "Cantidad producida inválida.";
                return;
            }

            await TryTransitionAsync(() => order.RegistrarProduccion(produced), "RegistrarProduccion", order, "Producción registrada.");
        }

        private async Task CompleteAsync()
        {
            if (!TryEnsureSelectedOrder(out var order))
            {
                return;
            }

            await TryTransitionAsync(() => order.Completar(), "Finalizada", order, "Orden finalizada.");
        }

        private async Task CancelAsync()
        {
            if (!TryEnsureSelectedOrder(out var order))
            {
                return;
            }

            var reason = string.IsNullOrWhiteSpace(Observaciones) ? "Cancelada desde módulo de producción." : Observaciones;
            await TryTransitionAsync(() => order.Cancelar(reason), "Cancelada", order, "Orden cancelada.");
        }

        private void GoBack()
        {
            _navigationService.NavigateTo(_dashboardViewModel);
        }

        private bool TryEnsureSelectedOrder(out OrdenProduccion order)
        {
            order = null!;
            if (!CanManageOrders)
            {
                StatusMessage = "No tiene permisos para gestionar órdenes.";
                return false;
            }

            if (SelectedOrder is null)
            {
                StatusMessage = "Debe seleccionar una orden.";
                return false;
            }

            order = SelectedOrder;
            return true;
        }

        private async Task TryTransitionAsync(Action transition, string action, OrdenProduccion order, string successMessage)
        {
            try
            {
                var previousState = order.Estado;
                var observations = Observaciones?.Trim() ?? string.Empty;
                if (!string.Equals(order.Observaciones, observations, StringComparison.Ordinal))
                {
                    order.ActualizarObservaciones(observations);
                }

                transition();
                await ApplyStockMovementAsync(previousState, order);
                _unitOfWork.OrdenesProduccion.Update(order);
                await _unitOfWork.SaveChangesAsync();
                await RegisterAuditAsync(action, order, successMessage);
                OnPropertyChanged(nameof(EstadoActual));
                RefreshIndicators();
                StatusMessage = successMessage;
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
            }
        }

        private void RefreshIndicators()
        {
            OnPropertyChanged(nameof(OrdersInProcessCount));
            OnPropertyChanged(nameof(OrdersCompletedCount));
            OnPropertyChanged(nameof(EstimatedOrdersCost));
            OnPropertyChanged(nameof(CriticalStockCount));
        }

        private async Task ApplyStockMovementAsync(EstadoOrdenProduccion previousState, OrdenProduccion order)
        {
            var recipeItems = await _unitOfWork.RecetaProductoItems.ListByProductIdAsync(order.ProductoId);
            if (!recipeItems.Any())
            {
                return;
            }

            var shouldConsume = previousState != EstadoOrdenProduccion.EnProceso
                && (order.Estado == EstadoOrdenProduccion.EnProceso || order.Estado == EstadoOrdenProduccion.Finalizada);

            var shouldRelease = previousState == EstadoOrdenProduccion.EnProceso
                && (order.Estado == EstadoOrdenProduccion.Borrador || order.Estado == EstadoOrdenProduccion.Cancelada);

            if (!shouldConsume && !shouldRelease)
            {
                return;
            }

            foreach (var recipeItem in recipeItems)
            {
                var resource = await _unitOfWork.Recursos.GetByIdAsync(recipeItem.RecursoId);
                if (resource is null)
                {
                    throw new InvalidOperationException($"No se encontró el insumo {recipeItem.RecursoId}.");
                }

                var requiredQuantity = recipeItem.Cantidad * order.CantidadPlaneada;

                if (shouldConsume)
                {
                    if (resource.StockActual < requiredQuantity)
                    {
                        throw new InvalidOperationException($"Stock insuficiente para {resource.Nombre}. Requerido: {requiredQuantity:0.##}, disponible: {resource.StockActual:0.##}.");
                    }

                    resource.StockActual -= requiredQuantity;
                }
                else
                {
                    resource.StockActual += requiredQuantity;
                }

                resource.FechaUltimaActualizacion = DateTime.UtcNow;
                _unitOfWork.Recursos.Update(resource);
            }
        }

        private string GenerateCode()
        {
            var sequence = Orders.Count(x => x.CreatedAt.Date == DateTime.UtcNow.Date) + 1;
            return $"OP-{DateTime.UtcNow:yyyyMMdd}-{sequence:000}";
        }

        private Task RegisterAuditAsync(string action, OrdenProduccion order, string description)
        {
            return _auditLogService.RegisterAsync(new AuditLog
            {
                Usuario = AuthSession.Current?.Usuario.Email ?? "desktop-user",
                Modulo = "Produccion",
                Accion = action,
                Entidad = "OrdenProduccion",
                IdEntidad = order.Id.ToString(),
                Descripcion = description,
                Equipo = Environment.MachineName
            });
        }
    }
}
