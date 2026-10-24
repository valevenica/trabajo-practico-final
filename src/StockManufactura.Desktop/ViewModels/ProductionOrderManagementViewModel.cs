using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
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
        private readonly IShortageService _shortageService;
        private int _criticalStockCount;

        [ObservableProperty] private Producto? _selectedProduct;
        [ObservableProperty] private ClientOption? _selectedClientOption = ClientOption.SinCliente;
        [ObservableProperty] private OrdenProduccion? _selectedOrder;
        [ObservableProperty] private string _cantidadPlaneada = "0";
        [ObservableProperty] private string _observaciones = string.Empty;
        [ObservableProperty] private string _statusMessage = string.Empty;
        [ObservableProperty] private string _codigoInput = string.Empty;

        public ProductionOrderManagementViewModel(
            IUnitOfWork unitOfWork,
            IAuditLogService auditLogService,
            NavigationService navigationService,
            DashboardViewModel dashboardViewModel,
            IShortageService shortageService)
        {
            _shortageService = shortageService ?? throw new ArgumentNullException(nameof(shortageService));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _dashboardViewModel = dashboardViewModel ?? throw new ArgumentNullException(nameof(dashboardViewModel));

            Products = new ObservableCollection<Producto>();
            Orders = new ObservableCollection<OrdenProduccion>();
            Clients = new ObservableCollection<Cliente>();
            ClientOptions = new ObservableCollection<ClientOption> { ClientOption.SinCliente };

            LoadCommand = new AsyncRelayCommand(LoadAsync);
            CreateOrderCommand = new AsyncRelayCommand(CreateOrderAsync);
            SetDraftCommand = new AsyncRelayCommand(SetDraftAsync);
            PlanifyCommand = new AsyncRelayCommand(PlanifyAsync);
            StartCommand = new AsyncRelayCommand(StartAsync);
            CompleteCommand = new AsyncRelayCommand(CompleteAsync);
            CancelCommand = new AsyncRelayCommand(CancelAsync);
            DeleteOrderCommand = new AsyncRelayCommand(DeleteOrderAsync);
            AssignClientCommand = new AsyncRelayCommand(AssignClientAsync);
            BackCommand = new RelayCommand(GoBack);

            _ = LoadAsync();
        }

        public ObservableCollection<Producto> Products { get; }
        public ObservableCollection<OrdenProduccion> Orders { get; }

        /// <summary>Todos los clientes, incluidos los inactivos, para mostrar el nombre en el listado de pedidos.</summary>
        public ObservableCollection<Cliente> Clients { get; }

        /// <summary>Consumidor final más los clientes activos, para elegir el cliente de un pedido.</summary>
        public ObservableCollection<ClientOption> ClientOptions { get; }

        public ICommand LoadCommand { get; }
        public ICommand CreateOrderCommand { get; }
        public ICommand SetDraftCommand { get; }
        public ICommand PlanifyCommand { get; }
        public ICommand StartCommand { get; }
        public ICommand CompleteCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand DeleteOrderCommand { get; }
        public ICommand AssignClientCommand { get; }
        public ICommand BackCommand { get; }

        public bool CanManageOrders => AuthSession.Current?.TienePermiso("PRODUCTOS_EDITAR") == true;
        public string CurrentUserName => AuthSession.Current?.Usuario?.Nombre ?? "Administrador";
        /// <summary>Productos e insumos en faltante, con el mismo criterio que el panel principal y la pantalla de faltantes.</summary>
        public int CriticalStockCount => _criticalStockCount;
        public int OrdersInProcessCount => Orders.Count(x => x.Estado == EstadoOrdenProduccion.EnProceso);
        public int OrdersCompletedCount => Orders.Count(x => x.Estado == EstadoOrdenProduccion.Finalizada);
        /// <summary>Costo estimado de las órdenes creadas en el mes en curso, sin las canceladas.</summary>
        public decimal EstimatedOrdersCost => Orders
            .Where(order => order.Estado != EstadoOrdenProduccion.Cancelada && IsCurrentMonth(order.CreatedAt))
            .Sum(order =>
        {
            var product = Products.FirstOrDefault(x => x.Id == order.ProductoId);
            return (product?.CostoFabricacionActual ?? 0m) * order.CantidadPlaneada;
        });
        public decimal MonthlySalesTotal => Orders
            .Where(order => order.Estado == EstadoOrdenProduccion.Finalizada)
            .Where(order => IsCurrentMonth(order.FechaFin ?? order.CreatedAt))
            .Sum(order =>
            {
                var product = Products.FirstOrDefault(x => x.Id == order.ProductoId);
                return (product?.PrecioSugeridoActual ?? 0m) * order.CantidadPlaneada;
            });

        public string EstadoActual => SelectedOrder?.Estado.ToString() ?? "Sin selección";

        public string StockAvailabilityMessage
        {
            get
            {
                if (SelectedOrder is null)
                {
                    return string.Empty;
                }

                var isWaiting = SelectedOrder.Estado == EstadoOrdenProduccion.Borrador || SelectedOrder.Estado == EstadoOrdenProduccion.Planificada;
                if (!isWaiting)
                {
                    return string.Empty;
                }

                var product = Products.FirstOrDefault(x => x.Id == SelectedOrder.ProductoId);
                if (product is null)
                {
                    return string.Empty;
                }

                var reserved = GetReservedQuantityForProduct(product.Id);
                var disponible = product.StockActual - reserved;
                return disponible < 0
                    ? $"Stock insuficiente: faltan {-disponible:0.##} unidades de {product.Nombre} (considerando otras órdenes en espera)."
                    : $"Stock disponible: {product.StockActual:0.##} en stock, {reserved:0.##} reservadas en total para órdenes en espera.";
            }
        }

        partial void OnSelectedOrderChanged(OrdenProduccion? value)
        {
            OnPropertyChanged(nameof(StockAvailabilityMessage));

            if (value is null)
            {
                return;
            }

            SelectedProduct = Products.FirstOrDefault(x => x.Id == value.ProductoId);
            SelectedClientOption = GetClientOption(value.ClienteId);
            CantidadPlaneada = value.CantidadPlaneada.ToString("0.00", CultureInfo.InvariantCulture);
            Observaciones = value.Observaciones;
            OnPropertyChanged(nameof(EstadoActual));
        }

        private async Task LoadAsync()
        {
            try
            {
                var (products, orders, clients) = await Task.Run(async () =>
                {
                    var p = await _unitOfWork.Productos.ListAsync();
                    var o = await _unitOfWork.OrdenesProduccion.ListByCreatedDescAsync();
                    var c = await _unitOfWork.Clientes.ListAsync();
                    return (p, o, c);
                });
                _criticalStockCount = (await _shortageService.GetShortagesAsync()).TotalCount;

                var selectedClientId = SelectedClientOption?.Id;
                Clients.Clear();
                foreach (var client in clients.OrderBy(x => x.Nombre))
                    Clients.Add(client);
                ClientOptions.Clear();
                ClientOptions.Add(ClientOption.SinCliente);
                foreach (var client in Clients.Where(x => x.Activo))
                    ClientOptions.Add(new ClientOption(client.Id, client.Nombre));
                SelectedClientOption = GetClientOption(selectedClientId);

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

            var codigo = string.IsNullOrWhiteSpace(CodigoInput) ? GenerateCode() : CodigoInput.Trim().ToUpperInvariant();
            var order = new OrdenProduccion(codigo, SelectedProduct.Id, planned, Observaciones.Trim(), SelectedClientOption?.Id);
            await _unitOfWork.OrdenesProduccion.AddAsync(order);
            await _unitOfWork.SaveChangesAsync();
            await RegisterAuditAsync("Crear", order, "Alta de orden de producción.");

            CodigoInput = codigo; // keep code visible so user can reuse it for next product
            await LoadAsync();
            SelectedOrder = Orders.FirstOrDefault(x => x.Id == order.Id);
            RefreshIndicators();
            OnPropertyChanged(nameof(StockAvailabilityMessage));

            await WarnIfStockInsufficientAsync(codigo, order);
        }

        private async Task WarnIfStockInsufficientAsync(string codigo, OrdenProduccion order)
        {
            var createdProduct = Products.FirstOrDefault(x => x.Id == order.ProductoId);
            if (createdProduct is null)
            {
                StatusMessage = $"Orden {codigo} creada.";
                return;
            }

            var reserved = GetReservedQuantityForProduct(createdProduct.Id);
            var disponible = createdProduct.StockActual - reserved;
            if (disponible >= 0)
            {
                StatusMessage = $"Orden {codigo} creada. Para agregar otro producto a la misma orden, mantené el código y seleccioná otro producto.";
                return;
            }

            var faltante = -disponible;
            var warning = $"La orden {codigo} quedó en espera: falta stock de {createdProduct.Nombre} para completar el pedido (faltan {faltante:0.##} unidades, considerando otras órdenes en espera de este producto).";

            // Validación informativa: ¿alcanzan los insumos para fabricar las unidades faltantes? No se descuenta nada acá;
            // el único lugar que efectivamente descuenta insumos es StockIntakeService al registrar un ingreso de producto.
            var recipeItems = await _unitOfWork.RecetaProductoItems.ListByProductIdAsync(createdProduct.Id);
            var insumoShortages = new List<string>();
            foreach (var item in recipeItems.Where(x => x.RecursoId.HasValue))
            {
                var resource = await _unitOfWork.Recursos.GetByIdAsync(item.RecursoId!.Value);
                if (resource is null || resource.EsServicio)
                {
                    continue;
                }

                var requiredForShortage = item.Cantidad * faltante;
                if (resource.StockActual < requiredForShortage)
                {
                    var missing = requiredForShortage - resource.StockActual;
                    insumoShortages.Add($"{resource.Nombre} (faltan {missing:0.##} {resource.UnidadMedida})");
                }
            }

            if (insumoShortages.Count > 0)
            {
                warning += $" Además, no alcanzan los insumos para fabricar esas {faltante:0.##} unidades: {string.Join(", ", insumoShortages)}.";
            }

            StatusMessage = warning;
            MessageBox.Show(warning, "Stock insuficiente", MessageBoxButton.OK, MessageBoxImage.Warning);
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

        private async Task AssignClientAsync()
        {
            if (!CanManageOrders)
            {
                StatusMessage = "No tiene permisos para gestionar órdenes.";
                return;
            }

            if (SelectedOrder is null)
            {
                StatusMessage = "Seleccione el pedido al que quiere asignar el cliente.";
                return;
            }

            var order = SelectedOrder;
            var clientId = SelectedClientOption?.Id;
            try
            {
                order.AsignarCliente(clientId);
                _unitOfWork.OrdenesProduccion.Update(order);
                await _unitOfWork.SaveChangesAsync();
                var nombre = SelectedClientOption?.Nombre ?? ClientOption.ConsumidorFinal;
                await RegisterAuditAsync("AsignarCliente", order, $"Cliente del pedido: «{nombre}».");
                await LoadAsync();
                SelectedOrder = Orders.FirstOrDefault(x => x.Id == order.Id);
                StatusMessage = $"Pedido {order.Codigo} asignado al cliente «{nombre}».";
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
            }
        }

        private ClientOption GetClientOption(Guid? clientId)
        {
            if (clientId is null)
            {
                return ClientOption.SinCliente;
            }

            var option = ClientOptions.FirstOrDefault(x => x.Id == clientId);
            if (option is not null)
            {
                return option;
            }

            // Cliente desactivado despues de crear el pedido: se muestra igual, marcado, para no ocultar el dato.
            var inactive = Clients.FirstOrDefault(x => x.Id == clientId);
            option = new ClientOption(clientId, inactive is null ? "(cliente no disponible)" : inactive.Nombre + " (inactivo)");
            ClientOptions.Add(option);
            return option;
        }

        private async Task DeleteOrderAsync()
        {
            if (!TryEnsureSelectedOrder(out var order))
            {
                return;
            }

            if (order.Estado == EstadoOrdenProduccion.EnProceso)
            {
                StatusMessage = "No se puede eliminar una orden en proceso. Cancelala primero para liberar el stock reservado.";
                return;
            }

            if (order.Estado == EstadoOrdenProduccion.Finalizada)
            {
                StatusMessage = "No se pueden eliminar órdenes finalizadas; forman parte del historial.";
                return;
            }

            var confirm = MessageBox.Show(
                $"¿Eliminar la orden '{order.Codigo}'? Esta acción no se puede deshacer.",
                "Confirmar eliminación", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                _unitOfWork.OrdenesProduccion.Delete(order);
                await _unitOfWork.SaveChangesAsync();
                await RegisterAuditAsync("Eliminar", order, "Orden eliminada.");

                SelectedOrder = null;
                await LoadAsync();
                RefreshIndicators();
                StatusMessage = "Orden eliminada.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"No se pudo eliminar: {ex.Message}";
            }
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
            OnPropertyChanged(nameof(MonthlySalesTotal));
            OnPropertyChanged(nameof(CriticalStockCount));
            OnPropertyChanged(nameof(StockAvailabilityMessage));
        }

        private async Task ApplyStockMovementAsync(EstadoOrdenProduccion previousState, OrdenProduccion order)
        {
            // Antes, planificar una orden en proceso no reintegraba las unidades y al iniciarla de nuevo se descontaban dos veces.
            var movimiento = OrdenProduccion.MovimientoDeStock(previousState, order.Estado);
            if (movimiento == 0)
            {
                return;
            }

            var shouldDispatch = movimiento < 0;

            var product = await _unitOfWork.Productos.GetByIdAsync(order.ProductoId);
            if (product is null)
            {
                throw new InvalidOperationException("No se encontró el producto de la orden.");
            }

            if (shouldDispatch)
            {
                if (product.StockActual < order.CantidadPlaneada)
                {
                    var faltante = order.CantidadPlaneada - product.StockActual;
                    throw new InvalidOperationException($"Stock insuficiente de {product.Nombre} para completar el pedido. Faltan {faltante:0.##} unidades.");
                }

                product.StockActual -= order.CantidadPlaneada;
            }
            else
            {
                product.StockActual += order.CantidadPlaneada;
            }

            _unitOfWork.Productos.Update(product);
        }

        private decimal GetReservedQuantityForProduct(Guid productId)
        {
            return Orders
                .Where(o => (o.Estado == EstadoOrdenProduccion.Borrador || o.Estado == EstadoOrdenProduccion.Planificada)
                            && o.ProductoId == productId)
                .Sum(o => o.CantidadPlaneada);
        }

        private static bool IsCurrentMonth(DateTime value)
        {
            var now = DateTime.UtcNow;
            return value.Year == now.Year && value.Month == now.Month;
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
