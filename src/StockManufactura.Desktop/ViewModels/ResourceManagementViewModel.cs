using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
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

        public ResourceManagementViewModel(IResourcePricingService resourcePricingService, IMonetaryConfigurationService monetaryConfigurationService)
        {
            _resourcePricingService = resourcePricingService;
            _monetaryConfigurationService = monetaryConfigurationService;
            Resources = new ObservableCollection<Recurso>();
            PriceHistory = new ObservableCollection<ResourcePriceHistory>();
            LoadCommand = new AsyncRelayCommand(LoadAsync);
            SaveCommand = new AsyncRelayCommand(SaveAsync);
            SelectArsCommand = new RelayCommand(() => IsUsd = false);
            SelectUsdCommand = new RelayCommand(() => IsUsd = true);
            RecalculateCommand = new AsyncRelayCommand(RecalculateAsync);

            _ = LoadAsync();
        }

        public ObservableCollection<Recurso> Resources { get; }
        public ObservableCollection<ResourcePriceHistory> PriceHistory { get; }

        public ICommand LoadCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand SelectArsCommand { get; }
        public ICommand SelectUsdCommand { get; }
        public ICommand RecalculateCommand { get; }

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

        partial void OnIsUsdChanged(bool value)
        {
            OnPropertyChanged(nameof(UsdDetailsVisibility));
            OnPropertyChanged(nameof(IsArs));
            _ = RecalculateAsync();
        }

        partial void OnSelectedResourceChanged(Recurso? value)
        {
            if (value is null)
            {
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
        }

        private async Task LoadAsync()
        {
            try
            {
                var (resources, state) = await Task.Run(async () =>
                {
                    var r = await _resourcePricingService.GetResourcesAsync();
                    var s = await _monetaryConfigurationService.GetCurrentStateAsync();
                    return (r, s);
                });

                Resources.Clear();
                foreach (var resource in resources)
                    Resources.Add(resource);

                CotizacionVigente = state.CurrentRate.ToString("0.0000", CultureInfo.InvariantCulture);
                await RecalculateAsync();
                StatusMessage = "Recursos cargados.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error al cargar recursos: {ex.Message}";
            }
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
            CotizacionVigente = state.CurrentRate.ToString("0.0000", CultureInfo.InvariantCulture);
            var rate = state.CurrentRate <= 0 ? 1 : state.CurrentRate;
            var result = IsUsd ? parsedPrice * rate : parsedPrice;
            CostoEquivalentePesos = result.ToString("0.0000", CultureInfo.InvariantCulture);
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
    }
}
