using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Serilog;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StockManufactura.Application.Interfaces;
using StockManufactura.Application.Resources;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Desktop.ViewModels
{
    public sealed partial class ResourceManagementViewModel : ObservableObject
    {
        private readonly IResourcePricingService _resourcePricingService;
        private readonly IMonetaryConfigurationService _monetaryConfigurationService;
        private readonly IUnitOfWork _unitOfWork;

        [ObservableProperty]
        private Recurso? _selectedResource;

        [ObservableProperty]
        private string _codigo = string.Empty;

        [ObservableProperty]
        private string _nombre = string.Empty;

        [ObservableProperty]
        private string _descripcion = string.Empty;

        [ObservableProperty]
        private string _categoria = string.Empty;

        [ObservableProperty]
        private string _unidadMedida = string.Empty;

        [ObservableProperty]
        private decimal _stockActual;

        [ObservableProperty]
        private decimal _stockMinimo;

        [ObservableProperty]
        private string _precio = "0";

        [ObservableProperty]
        private bool _isUsd;

        [ObservableProperty]
        private string _cotizacionVigente = "0";

        [ObservableProperty]
        private string _costoEquivalentePesos = "0";

        [ObservableProperty]
        private string _observaciones = string.Empty;

        [ObservableProperty]
        private string _statusMessage = "Listo.";

        [ObservableProperty]
        private bool _activo = true;

        [ObservableProperty]
        private bool _isDirty;

        private bool _loading;

        [ObservableProperty]
        private Proveedor? _selectedNuevoProveedor;

        [ObservableProperty]
        private string _precioNuevoProveedor = "0";

        [ObservableProperty]
        private RecursoProveedorRow? _selectedProveedorDelInsumo;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private string _proveedorSearchText = string.Empty;

        private readonly CollectionViewSource _proveedoresViewSource = new();
        public ICollectionView ProveedoresView => _proveedoresViewSource.View;

        public ResourceManagementViewModel(
            IResourcePricingService resourcePricingService,
            IMonetaryConfigurationService monetaryConfigurationService,
            IUnitOfWork unitOfWork)
        {
            _resourcePricingService = resourcePricingService;
            _monetaryConfigurationService = monetaryConfigurationService;
            _unitOfWork = unitOfWork;
            Resources = new ObservableCollection<Recurso>();
            FilteredResources = new ObservableCollection<Recurso>();
            PriceHistory = new ObservableCollection<ResourcePriceHistory>();
            ProveedoresDisponibles = new ObservableCollection<Proveedor>();
            FilteredProveedoresDisponibles = new ObservableCollection<Proveedor>();
            ProveedoresDelInsumo = new ObservableCollection<RecursoProveedorRow>();

            _proveedoresViewSource.Source = ProveedoresDisponibles;
            _proveedoresViewSource.Filter += (s, e) =>
            {
                if (e.Item is Proveedor p)
                {
                    var q = ProveedorSearchText?.Trim().ToLower() ?? string.Empty;
                    e.Accepted = string.IsNullOrEmpty(q) || p.Nombre.ToLower().Contains(q);
                }
            };
            LoadCommand = new AsyncRelayCommand(LoadAsync);
            SaveCommand = new AsyncRelayCommand(SaveAsync);
            SelectArsCommand = new RelayCommand(() => IsUsd = false);
            SelectUsdCommand = new RelayCommand(() => IsUsd = true);
            RecalculateCommand = new AsyncRelayCommand(RecalculateAsync);
            AddProveedorCommand = new AsyncRelayCommand(AddProveedorAsync);
            RemoveProveedorCommand = new AsyncRelayCommand(RemoveProveedorAsync);
            SetPrioritarioCommand = new AsyncRelayCommand(SetPrioritarioAsync);
            RecalcularTodosUSDCommand = new AsyncRelayCommand(RecalcularTodosUSDAsync);

            _ = LoadAsync();
        }

        public ObservableCollection<Recurso> Resources { get; }
        public ObservableCollection<Recurso> FilteredResources { get; }
        public ObservableCollection<ResourcePriceHistory> PriceHistory { get; }
        public ObservableCollection<Proveedor> ProveedoresDisponibles { get; }
        public ObservableCollection<Proveedor> FilteredProveedoresDisponibles { get; }
        public ObservableCollection<RecursoProveedorRow> ProveedoresDelInsumo { get; }

        public ICommand LoadCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand SelectArsCommand { get; }
        public ICommand SelectUsdCommand { get; }
        public ICommand RecalculateCommand { get; }
        public ICommand AddProveedorCommand { get; }
        public ICommand RemoveProveedorCommand { get; }
        public ICommand SetPrioritarioCommand { get; }
        public ICommand RecalcularTodosUSDCommand { get; }

        public Visibility UsdDetailsVisibility => IsUsd ? Visibility.Visible : Visibility.Collapsed;
        public bool IsArs
        {
            get => !IsUsd;
            set
            {
                if (value)
                {
                    IsUsd = false;
                }
            }
        }

        partial void OnSearchTextChanged(string value)
        {
            UpdateFilteredResources();
        }

        partial void OnProveedorSearchTextChanged(string value)
        {
            ProveedoresView.Refresh();
        }

        partial void OnIsUsdChanged(bool value)
        {
            OnPropertyChanged(nameof(UsdDetailsVisibility));
            OnPropertyChanged(nameof(IsArs));
            _ = RecalculateAsync();
        }

        partial void OnSelectedResourceChanged(Recurso? value)
        {
            _loading = true;
            try
            {
                if (value is null)
                {
                    ProveedoresDelInsumo.Clear();
                    return;
                }

                Codigo = value.Codigo;
                Nombre = value.Nombre;
                Descripcion = value.Descripcion;
                Categoria = value.Categoria;
                UnidadMedida = value.UnidadMedida;
                StockActual = value.StockActual;
                StockMinimo = value.StockMinimo;
                Precio = value.Precio.ToString(CultureInfo.InvariantCulture);
                IsUsd = value.Moneda == Moneda.USD;
                Observaciones = value.Observaciones;
                Activo = value.Activo;
                _ = LoadHistoryAsync(value.Id);
                _ = LoadProveedoresDelInsumoAsync(value.Id);
            }
            finally
            {
                _loading = false;
                IsDirty = false;
            }
        }

        partial void OnCodigoChanged(string value) { if (!_loading) IsDirty = true; }
        partial void OnNombreChanged(string value) { if (!_loading) IsDirty = true; }
        partial void OnDescripcionChanged(string value) { if (!_loading) IsDirty = true; }
        partial void OnCategoriaChanged(string value) { if (!_loading) IsDirty = true; }
        partial void OnUnidadMedidaChanged(string value) { if (!_loading) IsDirty = true; }
        partial void OnStockActualChanged(decimal value) { if (!_loading) IsDirty = true; }
        partial void OnStockMinimoChanged(decimal value) { if (!_loading) IsDirty = true; }
        partial void OnPrecioChanged(string value) { if (!_loading) IsDirty = true; }
        partial void OnActivoChanged(bool value) { if (!_loading) IsDirty = true; }
        partial void OnObservacionesChanged(string value) { if (!_loading) IsDirty = true; }

        private async Task LoadAsync()
        {
            try
            {
                var r = await _resourcePricingService.GetResourcesAsync();
                var s = await _monetaryConfigurationService.GetCurrentStateAsync();
                var p = await _unitOfWork.Proveedores.ListAsync();

                Resources.Clear();
                foreach (var resource in r)
                    Resources.Add(resource);

                UpdateFilteredResources();
                ProveedoresDisponibles.Clear();
                foreach (var proveedor in p.Where(x => x.Activo).OrderBy(x => x.Nombre))
                    ProveedoresDisponibles.Add(proveedor);
                UpdateFilteredProveedores();

                CotizacionVigente = s.CurrentRate.ToString("0.00", CultureInfo.InvariantCulture);
                await RecalculateAsync();
                StatusMessage = "Recursos cargados.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error al cargar recursos: {ex.Message}";
            }
        }

        private void UpdateFilteredResources()
        {
            var search = SearchText.ToLower().Trim();
            var filtered = string.IsNullOrEmpty(search)
                ? Resources
                : Resources.Where(r => r.Codigo.ToLower().Contains(search) || r.Nombre.ToLower().Contains(search));

            FilteredResources.Clear();
            foreach (var resource in filtered)
                FilteredResources.Add(resource);
        }

        private void UpdateFilteredProveedores()
        {
            // No-op: filtering is handled by CollectionViewSource
        }

        private async Task SaveAsync()
        {
            if (!decimal.TryParse(Precio, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedPrice))
            {
                StatusMessage = "Precio inválido.";
                return;
            }

            var request = new ResourceUpsertRequest
            {
                ResourceId = SelectedResource?.Id,
                Codigo = Codigo,
                Nombre = Nombre,
                Descripcion = Descripcion,
                Categoria = Categoria,
                UnidadMedida = UnidadMedida,
                StockActual = StockActual,
                StockMinimo = StockMinimo,
                Precio = parsedPrice,
                Moneda = IsUsd ? Moneda.USD : Moneda.ARS,
                ProveedorHabitualId = SelectedResource?.ProveedorHabitualId,
                Observaciones = Observaciones,
                Activo = Activo,
                Usuario = "desktop-user"
            };

            var saved = await _resourcePricingService.UpsertResourceAsync(request);
            StatusMessage = "Precio del recurso guardado manualmente.";
            IsDirty = false;

            if (SelectedResource is null)
            {
                Resources.Add(saved);
            }
            else
            {
                var index = Resources.IndexOf(SelectedResource);
                if (index >= 0)
                {
                    Resources[index] = saved;
                }
            }

            SelectedResource = saved;
            await RecalculateAsync();
            await LoadHistoryAsync(saved.Id);
        }

        private async Task RecalculateAsync()
        {
            if (!decimal.TryParse(Precio, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedPrice))
            {
                CostoEquivalentePesos = "0";
                return;
            }

            var state = await _monetaryConfigurationService.GetCurrentStateAsync();
            CotizacionVigente = state.CurrentRate.ToString("0.00", CultureInfo.InvariantCulture);
            var rate = state.CurrentRate <= 0 ? 1 : state.CurrentRate;
            var result = IsUsd ? parsedPrice * rate : parsedPrice;
            CostoEquivalentePesos = result.ToString("0.00", CultureInfo.InvariantCulture);
        }

        private async Task LoadHistoryAsync(Guid resourceId)
        {
            var history = await _resourcePricingService.GetPriceHistoryAsync(resourceId);
            PriceHistory.Clear();
            foreach (var item in history)
            {
                PriceHistory.Add(item);
            }
        }

        private async Task LoadProveedoresDelInsumoAsync(Guid recursoId)
        {
            try
            {
                var items = await _unitOfWork.RecursoProveedores.ListByRecursoIdAsync(recursoId);

                ProveedoresDelInsumo.Clear();
                foreach (var item in items)
                    ProveedoresDelInsumo.Add(new RecursoProveedorRow(
                        item.Id, item.ProveedorId,
                        item.Proveedor.Nombre, item.Precio, item.EsPrioritario));
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error al cargar proveedores del insumo: {ex.Message}";
            }
        }

        private async Task AddProveedorAsync()
        {
            if (SelectedResource is null || SelectedNuevoProveedor is null)
            {
                StatusMessage = "Seleccioná un insumo y un proveedor.";
                return;
            }

            if (!decimal.TryParse(PrecioNuevoProveedor, NumberStyles.Number, CultureInfo.InvariantCulture, out var precio) || precio < 0)
            {
                StatusMessage = "Precio inválido.";
                return;
            }

            if (ProveedoresDelInsumo.Any(x => x.ProveedorId == SelectedNuevoProveedor.Id))
            {
                StatusMessage = "Ese proveedor ya está asociado a este insumo.";
                return;
            }

            try
            {
                var esPrimero = ProveedoresDelInsumo.Count == 0;
                var nuevo = new RecursoProveedor
                {
                    RecursoId = SelectedResource.Id,
                    ProveedorId = SelectedNuevoProveedor.Id,
                    Precio = precio,
                    EsPrioritario = esPrimero
                };
                await _unitOfWork.RecursoProveedores.AddAsync(nuevo);
                await _unitOfWork.SaveChangesAsync();

                if (esPrimero)
                    await UpdateRecursoPrecioAsync(SelectedResource, precio);

                await LoadProveedoresDelInsumoAsync(SelectedResource.Id);
                SelectedNuevoProveedor = null;
                PrecioNuevoProveedor = "0";
                StatusMessage = "Proveedor agregado.";
            }
            catch (Exception ex)
            {
                Log.Error(ex, "AddProveedorAsync failed");
                var msg = ex.Message;
                if (ex.InnerException != null) msg += " | " + ex.InnerException.Message;
                if (ex.InnerException?.InnerException != null) msg += " | " + ex.InnerException.InnerException.Message;
                StatusMessage = $"Error: {msg}";
            }
        }

        private async Task RemoveProveedorAsync()
        {
            if (SelectedResource is null || SelectedProveedorDelInsumo is null)
            {
                StatusMessage = "Seleccioná un proveedor para quitar.";
                return;
            }

            try
            {
                var items = await _unitOfWork.RecursoProveedores.ListByRecursoIdAsync(SelectedResource.Id);
                var target = items.FirstOrDefault(x => x.Id == SelectedProveedorDelInsumo.Id);
                if (target is null) return;

                _unitOfWork.RecursoProveedores.Delete(target);

                // Si era el prioritario, asignar el primero restante como nuevo prioritario
                decimal? nuevoPrecio = null;
                if (target.EsPrioritario)
                {
                    var remaining = items.Where(x => x.Id != target.Id).ToList();
                    if (remaining.Count > 0)
                    {
                        remaining[0].EsPrioritario = true;
                        _unitOfWork.RecursoProveedores.Update(remaining[0]);
                        nuevoPrecio = remaining[0].Precio;
                    }
                }

                await _unitOfWork.SaveChangesAsync();

                if (nuevoPrecio.HasValue)
                    await UpdateRecursoPrecioAsync(SelectedResource, nuevoPrecio.Value);

                await LoadProveedoresDelInsumoAsync(SelectedResource.Id);
                StatusMessage = "Proveedor quitado.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
        }

        private async Task SetPrioritarioAsync()
        {
            if (SelectedResource is null || SelectedProveedorDelInsumo is null)
            {
                StatusMessage = "Seleccioná un proveedor para marcar como prioritario.";
                return;
            }

            try
            {
                var items = await _unitOfWork.RecursoProveedores.ListByRecursoIdAsync(SelectedResource.Id);
                foreach (var item in items)
                {
                    item.EsPrioritario = item.Id == SelectedProveedorDelInsumo.Id;
                    _unitOfWork.RecursoProveedores.Update(item);
                }

                var prioritario = items.First(x => x.Id == SelectedProveedorDelInsumo.Id);
                await _unitOfWork.SaveChangesAsync();
                await UpdateRecursoPrecioAsync(SelectedResource, prioritario.Precio);

                await LoadProveedoresDelInsumoAsync(SelectedResource.Id);
                Precio = prioritario.Precio.ToString(CultureInfo.InvariantCulture);
                StatusMessage = $"Proveedor prioritario actualizado. Precio del insumo: {prioritario.Precio:0.00}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
        }

        private async Task UpdateRecursoPrecioAsync(Recurso recurso, decimal nuevoPrecio)
        {
            var request = new ResourceUpsertRequest
            {
                ResourceId = recurso.Id,
                Codigo = recurso.Codigo,
                Nombre = recurso.Nombre,
                Descripcion = recurso.Descripcion,
                Categoria = recurso.Categoria,
                UnidadMedida = recurso.UnidadMedida,
                StockActual = recurso.StockActual,
                StockMinimo = recurso.StockMinimo,
                Precio = nuevoPrecio,
                Moneda = recurso.Moneda,
                MotivoCambio = "Precio de proveedor prioritario actualizado",
                Observaciones = recurso.Observaciones,
                Activo = recurso.Activo,
                Usuario = "desktop-user"
            };
            await _resourcePricingService.UpsertResourceAsync(request);
        }

        private async Task RecalcularTodosUSDAsync()
        {
            try
            {
                StatusMessage = "Recalculando precios USD...";
                var count = await _resourcePricingService.RecalcularTodosUSDAsync("desktop-user");
                await LoadAsync();
                StatusMessage = $"Recalculo completado: {count} insumo(s) USD actualizados con cotización prioritaria.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error en recálculo: {ex.Message}";
            }
        }
    }

    public sealed record RecursoProveedorRow(
        Guid Id,
        Guid ProveedorId,
        string ProveedorNombre,
        decimal Precio,
        bool EsPrioritario);
}
