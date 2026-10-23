using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StockManufactura.Application.Interfaces;
using StockManufactura.Application.Products;
using StockManufactura.Application.Services;
using StockManufactura.Desktop.Infrastructure;
using StockManufactura.Desktop.Services;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Desktop.ViewModels
{
    public sealed partial class ProductManagementViewModel : ObservableObject
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditLogService _auditLogService;
        private readonly IProductCostService _productCostService;
        private readonly NavigationService _navigationService;
        private readonly DashboardViewModel _dashboardViewModel;

        [ObservableProperty]
        private Producto? _selectedProduct;

        [ObservableProperty]
        private string _codigo = string.Empty;

        [ObservableProperty]
        private string _nombre = string.Empty;

        [ObservableProperty]
        private string _descripcion = string.Empty;

        [ObservableProperty]
        private string _costoFabricacion = "0";

        [ObservableProperty]
        private string _margen = "0";

        [ObservableProperty]
        private string _precioSugerido = "0";

        [ObservableProperty]
        private string _stockActual = "0";

        [ObservableProperty]
        private string _stockMinimo = "0";

        [ObservableProperty]
        private bool _activo = true;

        [ObservableProperty]
        private string _observaciones = string.Empty;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _esNuevo = true;

        [ObservableProperty]
        private bool _isDirty;
        [ObservableProperty]
        private string _searchText = string.Empty;
        private bool _loading;

        [ObservableProperty]
        private ComponentOption? _selectedComponent;

        [ObservableProperty]
        private string _componentQuantity = "0";

        [ObservableProperty]
        private string _componentObservaciones = string.Empty;

        [ObservableProperty]
        private ProductRecipeRow? _selectedRecipeItem;

        private List<RecetaProductoItem> _currentRecipeItems = new();

        // Cotización con la que se muestran en pesos los insumos en dólares, la misma que usa el cálculo de costos.
        private decimal _cotizacion = 1m;

        public ObservableCollection<ProductRecipeRow> RecipeItems { get; } = new();
        public ObservableCollection<ComponentOption> AvailableComponents { get; } = new();

        public string CostoFabricacionCalculado => RecipeItems.Sum(x => x.CostoTotal).ToString("0.00", CultureInfo.InvariantCulture);

        public bool CanEditRecipeItems => SelectedProduct is not null && CanEditProducts;

        public string RecipeItemActionLabel => SelectedRecipeItem is null ? "+ Agregar" : "Guardar cambios";

        public ProductManagementViewModel(
            IUnitOfWork unitOfWork,
            IAuditLogService auditLogService,
            IProductCostService productCostService,
            NavigationService navigationService,
            DashboardViewModel dashboardViewModel,
            bool startInCreateMode = false)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
            _productCostService = productCostService ?? throw new ArgumentNullException(nameof(productCostService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _dashboardViewModel = dashboardViewModel ?? throw new ArgumentNullException(nameof(dashboardViewModel));

            Products = new ObservableCollection<Producto>();
            FilteredProducts = new ObservableCollection<Producto>();
            LoadCommand = new AsyncRelayCommand(LoadAsync);
            SaveCommand = new AsyncRelayCommand(SaveAsync);
            NewProductCommand = new RelayCommand(StartNewProduct);
            SaveRecipeItemCommand = new AsyncRelayCommand(SaveRecipeItemAsync);
            RemoveRecipeItemCommand = new AsyncRelayCommand<ProductRecipeRow>(RemoveRecipeItemAsync);
            CancelRecipeItemEditCommand = new RelayCommand(ResetRecipeItemForm);
            ViewCostHistoryCommand = new RelayCommand<Producto>(ViewCostHistory);
            DeleteProductCommand = new AsyncRelayCommand(DeleteProductAsync);
            BackCommand = new RelayCommand(GoBack);

            _ = LoadAsync();

            if (startInCreateMode)
            {
                StartNewProduct();
            }
        }

        public ObservableCollection<Producto> Products { get; }
        public ObservableCollection<Producto> FilteredProducts { get; }

        public ICommand LoadCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand NewProductCommand { get; }
        public ICommand SaveRecipeItemCommand { get; }
        public ICommand RemoveRecipeItemCommand { get; }
        public ICommand CancelRecipeItemEditCommand { get; }
        public ICommand ViewCostHistoryCommand { get; }
        public ICommand DeleteProductCommand { get; }
        public ICommand BackCommand { get; }

        public bool CanViewProducts => AuthSession.Current?.TienePermiso("PRODUCTOS_VER") == true || CanCreateProducts || CanEditProducts;
        public bool CanCreateProducts => AuthSession.Current?.TienePermiso("PRODUCTOS_CREAR") == true;
        public bool CanEditProducts => AuthSession.Current?.TienePermiso("PRODUCTOS_EDITAR") == true;

        partial void OnSearchTextChanged(string value)
        {
            UpdateFilteredProducts();
        }

        partial void OnSelectedProductChanged(Producto? value)
        {
            _loading = true;
            try
            {
                if (value is null)
                {
                    RecipeItems.Clear();
                    CostoFabricacion = "0.00";
                    StockActual = "0";
                    StockMinimo = "0";
                    OnPropertyChanged(nameof(CostoFabricacionCalculado));
                    return;
                }

                EsNuevo = false;
                Codigo = value.Codigo;
                Nombre = value.Nombre;
                Descripcion = value.Descripcion;
                CostoFabricacion = value.CostoFabricacionActual.ToString("0.00", CultureInfo.InvariantCulture);
                Margen = value.MargenActual.ToString("0.00", CultureInfo.InvariantCulture);
                PrecioSugerido = value.PrecioSugeridoActual.ToString("0.00", CultureInfo.InvariantCulture);
                StockActual = value.StockActual.ToString("0.##", CultureInfo.InvariantCulture);
                StockMinimo = value.StockMinimo.ToString("0.##", CultureInfo.InvariantCulture);
                Activo = value.Activo;
                Observaciones = value.Observaciones;

                _ = LoadRecipeItemsAsync(value.Id);
            }
            finally
            {
                _loading = false;
                IsDirty = false;
                OnPropertyChanged(nameof(CanEditRecipeItems));
            }
        }

        partial void OnCodigoChanged(string value) { if (!_loading) IsDirty = true; }
        partial void OnNombreChanged(string value) { if (!_loading) IsDirty = true; }
        partial void OnDescripcionChanged(string value) { if (!_loading) IsDirty = true; }
        partial void OnMargenChanged(string value)
        {
            if (_loading)
            {
                return;
            }

            IsDirty = true;
            RecalcularPrecioSugerido();
        }

        partial void OnPrecioSugeridoChanged(string value) { if (!_loading) IsDirty = true; }
        partial void OnStockActualChanged(string value) { if (!_loading) IsDirty = true; }
        partial void OnStockMinimoChanged(string value) { if (!_loading) IsDirty = true; }

        private void RecalcularPrecioSugerido()
        {
            if (!decimal.TryParse(CostoFabricacion, NumberStyles.Number, CultureInfo.InvariantCulture, out var costo))
            {
                return;
            }

            if (!decimal.TryParse(Margen, NumberStyles.Number, CultureInfo.InvariantCulture, out var margen))
            {
                return;
            }

            PrecioSugerido = (costo + costo * margen).ToString("0.00", CultureInfo.InvariantCulture);
        }
        partial void OnActivoChanged(bool value) { if (!_loading) IsDirty = true; }
        partial void OnObservacionesChanged(string value) { if (!_loading) IsDirty = true; }

        partial void OnSelectedRecipeItemChanged(ProductRecipeRow? value)
        {
            OnPropertyChanged(nameof(RecipeItemActionLabel));

            if (value is null)
            {
                return;
            }

            var entity = _currentRecipeItems.FirstOrDefault(x => x.Id == value.Id);
            if (entity is null)
            {
                return;
            }

            SelectedComponent = value.EsProducto
                ? AvailableComponents.FirstOrDefault(x => x.EsProducto && x.Id == entity.ComponenteProductoId)
                : AvailableComponents.FirstOrDefault(x => !x.EsProducto && x.Id == entity.RecursoId);
            ComponentQuantity = value.Cantidad.ToString("0.00", CultureInfo.InvariantCulture);
            ComponentObservaciones = entity.Observaciones;
        }

        private async Task LoadAsync()
        {
            if (!CanViewProducts)
            {
                StatusMessage = "No tiene permisos para ver productos.";
                return;
            }

            try
            {
                var (products, resources, rate) = await Task.Run(async () =>
                {
                    var p = await _unitOfWork.Productos.ListAsync();
                    var r = await _unitOfWork.Recursos.ListActivosAsync();
                    var c = await _unitOfWork.ExchangeRates.GetLatestAsync();
                    return (p, r, c);
                });
                _cotizacion = rate?.Valor ?? 1m;

                var orderedProducts = products.OrderBy(x => x.Nombre).ToArray();

                Products.Clear();
                foreach (var product in orderedProducts)
                {
                    Products.Add(product);
                }

                UpdateFilteredProducts();

                AvailableComponents.Clear();
                foreach (var resource in resources.OrderBy(x => x.Nombre))
                {
                    AvailableComponents.Add(new ComponentOption(resource.Id, resource.Nombre, false, resource.PrecioEnPesos(_cotizacion), resource.UnidadMedida));
                }
                foreach (var product in orderedProducts.Where(x => x.Activo))
                {
                    AvailableComponents.Add(new ComponentOption(product.Id, $"[Producto] {product.Nombre}", true, product.CostoFabricacionActual, "u."));
                }

                StatusMessage = "Productos cargados.";

                if (SelectedProduct is null)
                {
                    RecipeItems.Clear();
                    CostoFabricacion = "0.00";
                    OnPropertyChanged(nameof(CostoFabricacionCalculado));
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error al cargar productos: {ex.Message}";
            }
        }

        private void UpdateFilteredProducts()
        {
            var search = SearchText.ToLower().Trim();
            var filtered = string.IsNullOrEmpty(search)
                ? Products
                : Products.Where(p => p.Codigo.ToLower().Contains(search) || p.Nombre.ToLower().Contains(search));

            FilteredProducts.Clear();
            foreach (var product in filtered)
                FilteredProducts.Add(product);
        }

        private async Task SaveAsync()
        {
            if (EsNuevo && !CanCreateProducts)
            {
                StatusMessage = "No tiene permisos para crear productos.";
                return;
            }

            if (!EsNuevo && !CanEditProducts)
            {
                StatusMessage = "No tiene permisos para editar productos.";
                return;
            }

            if (string.IsNullOrWhiteSpace(Codigo) || string.IsNullOrWhiteSpace(Nombre))
            {
                StatusMessage = "Código y nombre son requeridos.";
                return;
            }

            if (!decimal.TryParse(Margen, NumberStyles.Number, CultureInfo.InvariantCulture, out var margen))
            {
                StatusMessage = "Margen inválido.";
                return;
            }

            if (!decimal.TryParse(PrecioSugerido, NumberStyles.Number, CultureInfo.InvariantCulture, out var precioSugerido))
            {
                StatusMessage = "Precio sugerido inválido.";
                return;
            }

            if (!decimal.TryParse(StockActual, NumberStyles.Number, CultureInfo.InvariantCulture, out var stockActual) || stockActual < 0)
            {
                StatusMessage = "Stock actual inválido.";
                return;
            }

            if (!decimal.TryParse(StockMinimo, NumberStyles.Number, CultureInfo.InvariantCulture, out var stockMinimo) || stockMinimo < 0)
            {
                StatusMessage = "Stock mínimo inválido.";
                return;
            }

            var codigoNormalizado = Codigo.Trim().ToUpperInvariant();

            try
            {
                Producto product;
                var costoCalculado = ParseCostoCalculado();
                if (EsNuevo)
                {
                    var existing = await _unitOfWork.Productos.GetByCodigoAsync(codigoNormalizado);
                    if (existing is not null)
                    {
                        StatusMessage = "Ya existe un producto con ese código.";
                        return;
                    }

                    product = new Producto
                    {
                        Codigo = codigoNormalizado,
                        Nombre = Nombre.Trim(),
                        Descripcion = Descripcion.Trim(),
                        CostoFabricacionActual = costoCalculado,
                        MargenActual = margen,
                        PrecioSugeridoActual = precioSugerido,
                        StockActual = stockActual,
                        StockMinimo = stockMinimo,
                        Activo = Activo,
                        FechaUltimoCalculo = DateTime.UtcNow,
                        Observaciones = Observaciones.Trim()
                    };

                    await _unitOfWork.Productos.AddAsync(product);
                    await _unitOfWork.SaveChangesAsync();
                    await RegisterAuditAsync("Crear", product, "Alta de producto.");

                    await _productCostService.RecalculateAffectedProductsAsync(new ProductRecalculationRequest
                    {
                        Usuario = AuthSession.Current?.Usuario?.Email ?? "desktop-user",
                        Motivo = "Alta de producto",
                        ProductoDisparadorId = product.Id
                    });

                    StatusMessage = "Producto creado. Ya podés agregar componentes.";
                }
                else
                {
                    if (SelectedProduct is null)
                    {
                        StatusMessage = "Debe seleccionar un producto.";
                        return;
                    }

                    var existing = await _unitOfWork.Productos.GetByCodigoAsync(codigoNormalizado);
                    if (existing is not null && existing.Id != SelectedProduct.Id)
                    {
                        StatusMessage = "Ya existe un producto con ese código.";
                        return;
                    }

                    // El listado se carga sin seguimiento (AsNoTracking), pero la consulta por código anterior
                    // deja la entidad seguida por el contexto. Se editan los valores sobre esa instancia seguida:
                    // adjuntar la del listado provocaba "another instance with the same key value is already
                    // being tracked" y el cambio (por ejemplo, del stock) no se guardaba.
                    product = existing ?? await _unitOfWork.Productos.GetByIdAsync(SelectedProduct.Id)
                        ?? throw new InvalidOperationException("El producto ya no existe; recargue el listado.");
                    product.Codigo = codigoNormalizado;
                    product.Nombre = Nombre.Trim();
                    product.Descripcion = Descripcion.Trim();
                    product.CostoFabricacionActual = costoCalculado;
                    product.MargenActual = margen;
                    product.PrecioSugeridoActual = precioSugerido;
                    product.StockActual = stockActual;
                    product.StockMinimo = stockMinimo;
                    product.Activo = Activo;
                    product.Observaciones = Observaciones.Trim();
                    product.FechaUltimoCalculo = DateTime.UtcNow;
                    product.UpdateTimestamp();

                    _unitOfWork.Productos.Update(product);
                    await _unitOfWork.SaveChangesAsync();
                    await RegisterAuditAsync("Editar", product, "Edición de producto.");
                    StatusMessage = "Producto actualizado.";
                }

                await LoadAsync();
                SelectedProduct = Products.FirstOrDefault(x => x.Id == product.Id);

                // Refrescar los campos del formulario directamente desde la entidad recien guardada,
                // sin depender de que reasignar SelectedProduct dispare OnSelectedProductChanged
                // (por ejemplo, si la seleccion de la lista no vuelve a levantar el evento).
                _loading = true;
                try
                {
                    CostoFabricacion = product.CostoFabricacionActual.ToString("0.00", CultureInfo.InvariantCulture);
                    Margen = product.MargenActual.ToString("0.00", CultureInfo.InvariantCulture);
                    PrecioSugerido = product.PrecioSugeridoActual.ToString("0.00", CultureInfo.InvariantCulture);
                    StockActual = product.StockActual.ToString("0.##", CultureInfo.InvariantCulture);
                    StockMinimo = product.StockMinimo.ToString("0.##", CultureInfo.InvariantCulture);
                }
                finally
                {
                    _loading = false;
                }

                IsDirty = false;
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
            }
        }

        private void StartNewProduct()
        {
            _loading = true;
            try
            {
                EsNuevo = true;
                SelectedProduct = null;
                Codigo = string.Empty;
                Nombre = string.Empty;
                Descripcion = string.Empty;
                CostoFabricacion = "0";
                Margen = "0";
                PrecioSugerido = "0";
                StockActual = "0";
                StockMinimo = "0";
                Activo = true;
                Observaciones = string.Empty;
                RecipeItems.Clear();
                OnPropertyChanged(nameof(CostoFabricacionCalculado));
                StatusMessage = "Alta de producto nueva. Guardá el producto para poder agregarle componentes.";
            }
            finally
            {
                _loading = false;
                IsDirty = true;
                OnPropertyChanged(nameof(CanEditRecipeItems));
            }
        }

        private async Task LoadRecipeItemsAsync(Guid productId)
        {
            var items = await _unitOfWork.RecetaProductoItems.ListByProductIdAsync(productId);
            _currentRecipeItems = items.ToList();

            var tramosByRecursoId = new Dictionary<Guid, IReadOnlyList<RecursoTramoPrecio>>();
            var specialResourceIds = items
                .Where(x => x.RecursoId.HasValue && x.Recurso is not null && x.Recurso.EsServicio)
                .Select(x => x.RecursoId!.Value)
                .Distinct();
            foreach (var recursoId in specialResourceIds)
            {
                tramosByRecursoId[recursoId] = await _unitOfWork.RecursoTramosPrecio.ListByRecursoIdAsync(recursoId);
            }

            RecipeItems.Clear();

            foreach (var item in items.OrderBy(x => x.RecursoId.HasValue ? x.Recurso!.Nombre : x.ComponenteProducto!.Nombre))
            {
                var esProducto = item.RecursoId is null;
                var nombre = esProducto ? $"[Producto] {item.ComponenteProducto!.Nombre}" : item.Recurso!.Nombre;
                var unidad = esProducto ? "u." : item.Recurso!.UnidadMedida;

                decimal unitCost;
                decimal subtotal;
                if (!esProducto && item.Recurso!.EsServicio && tramosByRecursoId.TryGetValue(item.Recurso.Id, out var tramos) && tramos.Count > 0)
                {
                    var resultado = TramoPrecioCalculator.Calcular(tramos, item.Cantidad);
                    unitCost = resultado.PrecioPromedio;
                    subtotal = decimal.Round(resultado.CostoTotal, 4, MidpointRounding.AwayFromZero);
                }
                else
                {
                    unitCost = esProducto ? item.ComponenteProducto!.CostoFabricacionActual : item.Recurso!.PrecioEnPesos(_cotizacion);
                    subtotal = decimal.Round(item.Cantidad * unitCost, 4, MidpointRounding.AwayFromZero);
                }

                RecipeItems.Add(new ProductRecipeRow(
                    item.Id,
                    nombre,
                    unidad,
                    item.Cantidad,
                    unitCost,
                    subtotal,
                    esProducto));
            }

            CostoFabricacion = CostoFabricacionCalculado;
            OnPropertyChanged(nameof(CostoFabricacionCalculado));
        }

        private async Task<decimal> CalcularCostoParcialAsync(ComponentOption component, decimal cantidad)
        {
            if (!component.EsProducto)
            {
                var recurso = await _unitOfWork.Recursos.GetByIdAsync(component.Id);
                if (recurso is not null && recurso.EsServicio)
                {
                    var tramos = await _unitOfWork.RecursoTramosPrecio.ListByRecursoIdAsync(component.Id);
                    if (tramos.Count > 0)
                    {
                        return decimal.Round(TramoPrecioCalculator.Calcular(tramos, cantidad).CostoTotal, 4, MidpointRounding.AwayFromZero);
                    }
                }
            }

            return decimal.Round(cantidad * component.PrecioUnitario, 4, MidpointRounding.AwayFromZero);
        }

        private async Task SaveRecipeItemAsync()
        {
            if (!CanEditProducts)
            {
                StatusMessage = "No tiene permisos para editar productos.";
                return;
            }

            if (SelectedProduct is null)
            {
                StatusMessage = "Guardá el producto antes de agregar componentes.";
                return;
            }

            if (SelectedComponent is null)
            {
                StatusMessage = "Seleccioná un insumo o producto para agregar.";
                return;
            }

            if (SelectedComponent.EsProducto && SelectedComponent.Id == SelectedProduct.Id)
            {
                StatusMessage = "Un producto no puede ser su propio componente.";
                return;
            }

            if (!decimal.TryParse(ComponentQuantity, NumberStyles.Number, CultureInfo.InvariantCulture, out var cantidad) || cantidad <= 0)
            {
                StatusMessage = "Cantidad inválida.";
                return;
            }

            var editingId = SelectedRecipeItem?.Id;
            var duplicate = SelectedComponent.EsProducto
                ? _currentRecipeItems.Any(x => x.ComponenteProductoId == SelectedComponent.Id && x.Id != editingId)
                : _currentRecipeItems.Any(x => x.RecursoId == SelectedComponent.Id && x.Id != editingId);
            if (duplicate)
            {
                StatusMessage = "Ese componente ya está en la receta del producto.";
                return;
            }

            var costoParcial = await CalcularCostoParcialAsync(SelectedComponent, cantidad);

            try
            {
                if (editingId.HasValue)
                {
                    var entity = _currentRecipeItems.FirstOrDefault(x => x.Id == editingId.Value);
                    if (entity is null)
                    {
                        StatusMessage = "El componente seleccionado ya no existe.";
                        ResetRecipeItemForm();
                        return;
                    }

                    entity.RecursoId = SelectedComponent.EsProducto ? null : SelectedComponent.Id;
                    entity.ComponenteProductoId = SelectedComponent.EsProducto ? SelectedComponent.Id : null;
                    entity.Cantidad = cantidad;
                    entity.CostoParcialManual = costoParcial;
                    entity.Observaciones = ComponentObservaciones.Trim();
                    entity.UpdateTimestamp();

                    _unitOfWork.RecetaProductoItems.Update(entity);
                    await _unitOfWork.SaveChangesAsync();
                    await RegisterRecipeAuditAsync("EditarItem", SelectedProduct.Id,
                        $"Componente editado: {SelectedComponent.Nombre} | Cantidad: {cantidad:0.00} | Costo parcial: {costoParcial:0.00}");

                    await RecalculateProductCostAsync(SelectedProduct.Id);
                    StatusMessage = "Componente actualizado.";
                }
                else
                {
                    var item = new RecetaProductoItem
                    {
                        ProductoId = SelectedProduct.Id,
                        RecursoId = SelectedComponent.EsProducto ? null : SelectedComponent.Id,
                        ComponenteProductoId = SelectedComponent.EsProducto ? SelectedComponent.Id : null,
                        Cantidad = cantidad,
                        CostoParcialManual = costoParcial,
                        Observaciones = ComponentObservaciones.Trim()
                    };

                    await _unitOfWork.RecetaProductoItems.AddAsync(item);
                    await _unitOfWork.SaveChangesAsync();
                    await RegisterRecipeAuditAsync("CrearItem", SelectedProduct.Id,
                        $"Componente agregado: {SelectedComponent.Nombre} | Cantidad: {cantidad:0.00} | Costo parcial: {costoParcial:0.00}");

                    await RecalculateProductCostAsync(SelectedProduct.Id);
                    StatusMessage = "Componente agregado.";
                }

                ResetRecipeItemForm();
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
            }
        }

        private void ResetRecipeItemForm()
        {
            SelectedRecipeItem = null;
            SelectedComponent = null;
            ComponentQuantity = "0";
            ComponentObservaciones = string.Empty;
        }

        private async Task RemoveRecipeItemAsync(ProductRecipeRow? row)
        {
            if (!CanEditProducts)
            {
                StatusMessage = "No tiene permisos para editar productos.";
                return;
            }

            if (SelectedProduct is null || row is null)
            {
                return;
            }

            try
            {
                var entity = await _unitOfWork.RecetaProductoItems.GetByIdAsync(row.Id);
                if (entity is null)
                {
                    return;
                }

                _unitOfWork.RecetaProductoItems.Delete(entity);
                await _unitOfWork.SaveChangesAsync();
                await RegisterRecipeAuditAsync("EliminarItem", SelectedProduct.Id,
                    $"Componente eliminado: {row.Recurso} | Cantidad: {row.Cantidad:0.00}");

                if (SelectedRecipeItem?.Id == row.Id)
                {
                    ResetRecipeItemForm();
                }

                await RecalculateProductCostAsync(SelectedProduct.Id);
                StatusMessage = "Componente eliminado.";
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
            }
        }

        private async Task RecalculateProductCostAsync(Guid productId)
        {
            await _productCostService.RecalculateAffectedProductsAsync(new ProductRecalculationRequest
            {
                Usuario = AuthSession.Current?.Usuario?.Email ?? "desktop-user",
                Motivo = "Actualizacion de componentes del producto",
                CambioReceta = true
            });

            await LoadAsync();
            SelectedProduct = Products.FirstOrDefault(x => x.Id == productId);
        }

        private decimal ParseCostoCalculado()
        {
            return decimal.TryParse(CostoFabricacionCalculado, NumberStyles.Number, CultureInfo.InvariantCulture, out var cost)
                ? cost
                : 0m;
        }

        private async Task DeleteProductAsync()
        {
            if (!CanEditProducts)
            {
                StatusMessage = "No tiene permisos para editar productos.";
                return;
            }

            if (SelectedProduct is null)
            {
                StatusMessage = "Seleccioná un producto para eliminar.";
                return;
            }

            var allRecipeItems = await _unitOfWork.RecetaProductoItems.ListAsync();
            if (allRecipeItems.Any(x => x.ComponenteProductoId == SelectedProduct.Id))
            {
                StatusMessage = "No se puede eliminar: el producto está siendo usado como componente de otro producto.";
                return;
            }

            var orders = await _unitOfWork.OrdenesProduccion.ListAsync();
            if (orders.Any(x => x.ProductoId == SelectedProduct.Id))
            {
                StatusMessage = "No se puede eliminar: el producto tiene órdenes de producción asociadas.";
                return;
            }

            var confirm = MessageBox.Show(
                $"¿Eliminar el producto '{SelectedProduct.Nombre}'? Esta acción no se puede deshacer.",
                "Confirmar eliminación", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                // Se elimina la instancia seguida por el contexto (o se la carga), no la del listado sin seguimiento.
                var producto = await _unitOfWork.Productos.GetByIdAsync(SelectedProduct.Id) ?? SelectedProduct;
                _unitOfWork.Productos.Delete(producto);
                await _unitOfWork.SaveChangesAsync();
                await RegisterAuditAsync("Eliminar", producto, "Producto eliminado.");

                SelectedProduct = null;
                await LoadAsync();
                StatusMessage = "Producto eliminado.";
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

        private void ViewCostHistory(Producto? product)
        {
            var target = product ?? SelectedProduct;
            if (target is null)
            {
                StatusMessage = "Seleccioná un producto para ver su historial.";
                return;
            }

            _navigationService.NavigateTo(new ProductCostHistoryViewModel(
                _unitOfWork, _productCostService, _navigationService, _dashboardViewModel, target, this));
        }

        private Task RegisterAuditAsync(string action, Producto product, string description)
        {
            return _auditLogService.RegisterAsync(new AuditLog
            {
                Usuario = AuthSession.Current?.Usuario?.Email ?? "desktop-user",
                Modulo = "Productos",
                Accion = action,
                Entidad = "Producto",
                IdEntidad = product.Id.ToString(),
                Descripcion = description,
                Equipo = Environment.MachineName
            });
        }

        private Task RegisterRecipeAuditAsync(string action, Guid productId, string description)
        {
            return _auditLogService.RegisterAsync(new AuditLog
            {
                Usuario = AuthSession.Current?.Usuario?.Email ?? "desktop-user",
                Modulo = "Productos",
                Accion = action,
                Entidad = "RecetaProductoItem",
                IdEntidad = productId.ToString(),
                Descripcion = description,
                Equipo = Environment.MachineName
            });
        }
    }

    public sealed record ProductRecipeRow(
        Guid Id,
        string Recurso,
        string Unidad,
        decimal Cantidad,
        decimal CostoUnitario,
        decimal CostoTotal,
        bool EsProducto);

    public sealed record ComponentOption(Guid Id, string Nombre, bool EsProducto, decimal PrecioUnitario, string Unidad)
    {
        public override string ToString() => Nombre;
    }
}
