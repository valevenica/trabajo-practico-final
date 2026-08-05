using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StockManufactura.Application.Interfaces;
using StockManufactura.Desktop.Infrastructure;
using StockManufactura.Desktop.Services;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Desktop.ViewModels
{
    public sealed partial class ProductManagementViewModel : ObservableObject
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditLogService _auditLogService;
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
        private bool _activo = true;

        [ObservableProperty]
        private string _observaciones = string.Empty;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _esNuevo = true;

        [ObservableProperty]
        private bool _isDirty;

        private bool _loading;

        public ObservableCollection<ProductRecipeRow> RecipeItems { get; } = new();

        public string CostoFabricacionCalculado => RecipeItems.Sum(x => x.CostoTotal).ToString("0.0000", CultureInfo.InvariantCulture);

        public ProductManagementViewModel(
            IUnitOfWork unitOfWork,
            IAuditLogService auditLogService,
            NavigationService navigationService,
            DashboardViewModel dashboardViewModel,
            bool startInCreateMode = false)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _dashboardViewModel = dashboardViewModel ?? throw new ArgumentNullException(nameof(dashboardViewModel));

            Products = new ObservableCollection<Producto>();
            LoadCommand = new AsyncRelayCommand(LoadAsync);
            SaveCommand = new AsyncRelayCommand(SaveAsync);
            NewProductCommand = new RelayCommand(StartNewProduct);
            BackCommand = new RelayCommand(GoBack);

            _ = LoadAsync();

            if (startInCreateMode)
            {
                StartNewProduct();
            }
        }

        public ObservableCollection<Producto> Products { get; }

        public ICommand LoadCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand NewProductCommand { get; }
        public ICommand BackCommand { get; }

        public bool CanViewProducts => AuthSession.Current?.TienePermiso("PRODUCTOS_VER") == true || CanCreateProducts || CanEditProducts;
        public bool CanCreateProducts => AuthSession.Current?.TienePermiso("PRODUCTOS_CREAR") == true;
        public bool CanEditProducts => AuthSession.Current?.TienePermiso("PRODUCTOS_EDITAR") == true;

        partial void OnSelectedProductChanged(Producto? value)
        {
            _loading = true;
            try
            {
                if (value is null)
                {
                    RecipeItems.Clear();
                    CostoFabricacion = "0.0000";
                    OnPropertyChanged(nameof(CostoFabricacionCalculado));
                    return;
                }

                EsNuevo = false;
                Codigo = value.Codigo;
                Nombre = value.Nombre;
                Descripcion = value.Descripcion;
                CostoFabricacion = value.CostoFabricacionActual.ToString("0.0000", CultureInfo.InvariantCulture);
                Margen = value.MargenActual.ToString("0.0000", CultureInfo.InvariantCulture);
                PrecioSugerido = value.PrecioSugeridoActual.ToString("0.0000", CultureInfo.InvariantCulture);
                Activo = value.Activo;
                Observaciones = value.Observaciones;

                _ = LoadRecipeItemsAsync(value.Id);
            }
            finally
            {
                _loading = false;
                IsDirty = false;
            }
        }

        partial void OnCodigoChanged(string _) { if (!_loading) IsDirty = true; }
        partial void OnNombreChanged(string _) { if (!_loading) IsDirty = true; }
        partial void OnDescripcionChanged(string _) { if (!_loading) IsDirty = true; }
        partial void OnMargenChanged(string _) { if (!_loading) IsDirty = true; }
        partial void OnPrecioSugeridoChanged(string _) { if (!_loading) IsDirty = true; }
        partial void OnActivoChanged(bool _) { if (!_loading) IsDirty = true; }
        partial void OnObservacionesChanged(string _) { if (!_loading) IsDirty = true; }

        private async Task LoadAsync()
        {
            if (!CanViewProducts)
            {
                StatusMessage = "No tiene permisos para ver productos.";
                return;
            }

            try
            {
                var products = await Task.Run(async () =>
                    (await _unitOfWork.Productos.ListAsync()).OrderBy(x => x.Nombre).ToArray());

                Products.Clear();
                foreach (var product in products)
                {
                    Products.Add(product);
                }

                StatusMessage = "Productos cargados.";

                if (SelectedProduct is null)
                {
                    RecipeItems.Clear();
                    CostoFabricacion = "0.0000";
                    OnPropertyChanged(nameof(CostoFabricacionCalculado));
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error al cargar productos: {ex.Message}";
            }
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
                        Activo = Activo,
                        FechaUltimoCalculo = DateTime.UtcNow,
                        Observaciones = Observaciones.Trim()
                    };

                    await _unitOfWork.Productos.AddAsync(product);
                    await _unitOfWork.SaveChangesAsync();
                    await RegisterAuditAsync("Crear", product, "Alta de producto.");
                    StatusMessage = "Producto creado.";
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

                    product = SelectedProduct;
                    product.Codigo = codigoNormalizado;
                    product.Nombre = Nombre.Trim();
                    product.Descripcion = Descripcion.Trim();
                    product.CostoFabricacionActual = costoCalculado;
                    product.MargenActual = margen;
                    product.PrecioSugeridoActual = precioSugerido;
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
                Activo = true;
                Observaciones = string.Empty;
                RecipeItems.Clear();
                OnPropertyChanged(nameof(CostoFabricacionCalculado));
                StatusMessage = "Alta de producto nueva.";
            }
            finally
            {
                _loading = false;
                IsDirty = true;
            }
        }

        private async Task LoadRecipeItemsAsync(Guid productId)
        {
            var items = await Task.Run(async () =>
                await _unitOfWork.RecetaProductoItems.ListByProductIdAsync(productId));

            RecipeItems.Clear();

            foreach (var item in items.OrderBy(x => x.Recurso.Nombre))
            {
                var unitCost = item.Recurso.Precio;
                var subtotal = decimal.Round(item.Cantidad * unitCost, 4, MidpointRounding.AwayFromZero);
                RecipeItems.Add(new ProductRecipeRow(
                    item.Recurso.Nombre,
                    item.Recurso.UnidadMedida,
                    item.Cantidad,
                    unitCost,
                    subtotal));
            }

            CostoFabricacion = CostoFabricacionCalculado;
            OnPropertyChanged(nameof(CostoFabricacionCalculado));
        }

        private decimal ParseCostoCalculado()
        {
            return decimal.TryParse(CostoFabricacionCalculado, NumberStyles.Number, CultureInfo.InvariantCulture, out var cost)
                ? cost
                : 0m;
        }

        private void GoBack()
        {
            _navigationService.NavigateTo(_dashboardViewModel);
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
    }

    public sealed record ProductRecipeRow(
        string Recurso,
        string Unidad,
        decimal Cantidad,
        decimal CostoUnitario,
        decimal CostoTotal);
}
