using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StockManufactura.Application.Interfaces;
using StockManufactura.Application.Products;
using StockManufactura.Desktop.Infrastructure;
using StockManufactura.Desktop.Services;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Desktop.ViewModels
{
    public sealed partial class BomManagementViewModel : ObservableObject
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditLogService _auditLogService;
        private readonly IProductCostService _productCostService;
        private readonly NavigationService _navigationService;
        private readonly DashboardViewModel _dashboardViewModel;

        [ObservableProperty]
        private Producto? _selectedProduct;

        [ObservableProperty]
        private RecetaProductoItem? _selectedItem;

        [ObservableProperty]
        private Recurso? _selectedResource;

        [ObservableProperty]
        private string _cantidad = "0";

        [ObservableProperty]
        private string _costoParcialManual = "0";

        [ObservableProperty]
        private string _observaciones = string.Empty;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _isNewItem = true;

        public BomManagementViewModel(
            IUnitOfWork unitOfWork,
            IAuditLogService auditLogService,
            IProductCostService productCostService,
            NavigationService navigationService,
            DashboardViewModel dashboardViewModel)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
            _productCostService = productCostService ?? throw new ArgumentNullException(nameof(productCostService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _dashboardViewModel = dashboardViewModel ?? throw new ArgumentNullException(nameof(dashboardViewModel));

            Products = new ObservableCollection<Producto>();
            Resources = new ObservableCollection<Recurso>();
            Items = new ObservableCollection<RecetaProductoItem>();

            LoadCommand = new AsyncRelayCommand(LoadAsync);
            SaveItemCommand = new AsyncRelayCommand(SaveItemAsync);
            RemoveItemCommand = new AsyncRelayCommand(RemoveItemAsync);
            NewItemCommand = new RelayCommand(StartNewItem);
            BackCommand = new RelayCommand(GoBack);

            _ = LoadAsync();
        }

        public ObservableCollection<Producto> Products { get; }
        public ObservableCollection<Recurso> Resources { get; }
        public ObservableCollection<RecetaProductoItem> Items { get; }

        public ICommand LoadCommand { get; }
        public ICommand SaveItemCommand { get; }
        public ICommand RemoveItemCommand { get; }
        public ICommand NewItemCommand { get; }
        public ICommand BackCommand { get; }

        public bool CanViewRecipes => AuthSession.Current?.TienePermiso("PRODUCTOS_VER") == true
            || AuthSession.Current?.TienePermiso("PRODUCTOS_EDITAR") == true;

        public bool CanEditRecipes => AuthSession.Current?.TienePermiso("PRODUCTOS_EDITAR") == true;

        public string TotalReceta => Items.Sum(x => x.CostoParcialManual).ToString("0.0000", CultureInfo.InvariantCulture);

        partial void OnSelectedProductChanged(Producto? value)
        {
            _ = LoadItemsAsync(value?.Id);
        }

        partial void OnSelectedResourceChanged(Recurso? value)
        {
            UpdateCalculatedPartialCost();
        }

        partial void OnCantidadChanged(string value)
        {
            UpdateCalculatedPartialCost();
        }

        partial void OnSelectedItemChanged(RecetaProductoItem? value)
        {
            if (value is null)
            {
                return;
            }

            IsNewItem = false;
            SelectedResource = Resources.FirstOrDefault(x => x.Id == value.RecursoId);
            Cantidad = value.Cantidad.ToString("0.0000", CultureInfo.InvariantCulture);
            CostoParcialManual = value.CostoParcialManual.ToString("0.0000", CultureInfo.InvariantCulture);
            Observaciones = value.Observaciones;
        }

        private async Task LoadAsync()
        {
            if (!CanViewRecipes)
            {
                StatusMessage = "No tiene permisos para ver recetas.";
                return;
            }

            try
            {
                var (products, resources) = await Task.Run(async () =>
                {
                    var p = await _unitOfWork.Productos.ListActivosAsync();
                    var r = await _unitOfWork.Recursos.ListActivosAsync();
                    return (p, r);
                });

                Products.Clear();
                foreach (var product in products.OrderBy(x => x.Nombre))
                    Products.Add(product);

                Resources.Clear();
                foreach (var resource in resources.OrderBy(x => x.Nombre))
                    Resources.Add(resource);

                if (SelectedProduct is null && Products.Count > 0)
                    SelectedProduct = Products[0];

                if (SelectedResource is null && Resources.Count > 0)
                    SelectedResource = Resources[0];

                StatusMessage = "Recetas cargadas.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error al cargar recetas: {ex.Message}";
            }
        }

        private async Task LoadItemsAsync(Guid? productId)
        {
            Items.Clear();
            if (!productId.HasValue)
            {
                OnPropertyChanged(nameof(TotalReceta));
                return;
            }

            var items = await Task.Run(async () =>
                await _unitOfWork.RecetaProductoItems.ListByProductIdAsync(productId.Value));

            foreach (var item in items.OrderBy(x => x.Recurso.Nombre))
            {
                Items.Add(item);
            }

            OnPropertyChanged(nameof(TotalReceta));
        }

        private async Task SaveItemAsync()
        {
            if (!CanEditRecipes)
            {
                StatusMessage = "No tiene permisos para editar recetas.";
                return;
            }

            if (SelectedProduct is null)
            {
                StatusMessage = "Debe seleccionar un producto.";
                return;
            }

            if (SelectedResource is null)
            {
                StatusMessage = "Debe seleccionar un recurso.";
                return;
            }

            if (!decimal.TryParse(Cantidad, NumberStyles.Number, CultureInfo.InvariantCulture, out var cantidad) || cantidad <= 0)
            {
                StatusMessage = "Cantidad inválida.";
                return;
            }

            var costoParcial = decimal.Round(cantidad * SelectedResource.Precio, 4, MidpointRounding.AwayFromZero);

            try
            {
                RecetaProductoItem item;
                if (IsNewItem)
                {
                    var duplicate = Items.FirstOrDefault(x => x.RecursoId == SelectedResource.Id);
                    if (duplicate is not null)
                    {
                        StatusMessage = "Ese recurso ya está en la receta del producto.";
                        return;
                    }

                    item = new RecetaProductoItem
                    {
                        ProductoId = SelectedProduct.Id,
                        RecursoId = SelectedResource.Id,
                        Cantidad = cantidad,
                        CostoParcialManual = costoParcial,
                        Observaciones = Observaciones.Trim()
                    };

                    await _unitOfWork.RecetaProductoItems.AddAsync(item);
                    await _unitOfWork.SaveChangesAsync();
                    await RegisterAuditAsync("CrearItem", item, $"Insumo agregado: {item.Recurso?.Nombre ?? SelectedResource!.Nombre} | Cantidad: {item.Cantidad:0.0000} | Costo parcial: {item.CostoParcialManual:0.0000}", SelectedProduct!.Id);
                    StatusMessage = "Item agregado a la receta.";
                }
                else
                {
                    if (SelectedItem is null)
                    {
                        StatusMessage = "Debe seleccionar un item de receta.";
                        return;
                    }

                    var duplicate = Items.FirstOrDefault(x => x.RecursoId == SelectedResource.Id && x.Id != SelectedItem.Id);
                    if (duplicate is not null)
                    {
                        StatusMessage = "Ese recurso ya está en la receta del producto.";
                        return;
                    }

                    item = SelectedItem;
                    item.RecursoId = SelectedResource.Id;
                    item.Cantidad = cantidad;
                    item.CostoParcialManual = costoParcial;
                    item.Observaciones = Observaciones.Trim();
                    item.UpdateTimestamp();

                    _unitOfWork.RecetaProductoItems.Update(item);
                    await _unitOfWork.SaveChangesAsync();
                    await RegisterAuditAsync("EditarItem", item, $"Insumo modificado: {item.Recurso?.Nombre ?? SelectedResource!.Nombre} | Cantidad: {item.Cantidad:0.0000} | Costo parcial: {item.CostoParcialManual:0.0000}", SelectedProduct!.Id);
                    StatusMessage = "Item de receta actualizado.";
                }

                await RecalculateProductCostAsync(SelectedProduct);
                await LoadItemsAsync(SelectedProduct.Id);
                SelectedItem = Items.FirstOrDefault(x => x.Id == item.Id);
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
            }
        }

        private async Task RemoveItemAsync()
        {
            if (!CanEditRecipes)
            {
                StatusMessage = "No tiene permisos para editar recetas.";
                return;
            }

            if (SelectedProduct is null || SelectedItem is null)
            {
                StatusMessage = "Debe seleccionar un item para eliminar.";
                return;
            }

            _unitOfWork.RecetaProductoItems.Delete(SelectedItem);
            await _unitOfWork.SaveChangesAsync();
            await RegisterAuditAsync("EliminarItem", SelectedItem, $"Insumo eliminado: {SelectedItem.Recurso?.Nombre ?? "?"} | Cantidad: {SelectedItem.Cantidad:0.0000}", SelectedProduct!.Id);

            await RecalculateProductCostAsync(SelectedProduct);
            StartNewItem();
            await LoadItemsAsync(SelectedProduct.Id);
            StatusMessage = "Item eliminado.";
        }

        private void StartNewItem()
        {
            IsNewItem = true;
            SelectedItem = null;
            if (Resources.Count > 0)
            {
                SelectedResource = Resources[0];
            }

            Cantidad = "0";
            CostoParcialManual = "0";
            Observaciones = string.Empty;
            StatusMessage = "Nuevo item de receta.";
        }

        private void UpdateCalculatedPartialCost()
        {
            if (SelectedResource is null)
            {
                CostoParcialManual = "0";
                return;
            }

            if (!decimal.TryParse(Cantidad, NumberStyles.Number, CultureInfo.InvariantCulture, out var cantidad) || cantidad < 0)
            {
                CostoParcialManual = "0";
                return;
            }

            var costoParcial = decimal.Round(cantidad * SelectedResource.Precio, 4, MidpointRounding.AwayFromZero);
            CostoParcialManual = costoParcial.ToString("0.0000", CultureInfo.InvariantCulture);
        }

        private void GoBack()
        {
            _navigationService.NavigateTo(_dashboardViewModel);
        }

        private async Task RecalculateProductCostAsync(Producto product)
        {
            await _productCostService.RecalculateAffectedProductsAsync(new ProductRecalculationRequest
            {
                Usuario = AuthSession.Current?.Usuario?.Email ?? "desktop-user",
                Motivo = "Actualizacion de receta BOM",
                CambioReceta = true
            });

            await RegisterAuditAsync("RecalculoCostos", null, $"Recalculo de costos por cambios en receta | Producto: {product.Codigo}", product.Id);
        }

        private Task RegisterAuditAsync(string action, RecetaProductoItem? item, string description, Guid? productId = null)
        {
            return _auditLogService.RegisterAsync(new AuditLog
            {
                Usuario = AuthSession.Current?.Usuario?.Email ?? "desktop-user",
                Modulo = "Recetas",
                Accion = action,
                Entidad = "RecetaProductoItem",
                IdEntidad = (productId ?? SelectedProduct?.Id ?? item?.ProductoId)?.ToString() ?? string.Empty,
                Descripcion = description,
                Equipo = Environment.MachineName
            });
        }
    }
}
