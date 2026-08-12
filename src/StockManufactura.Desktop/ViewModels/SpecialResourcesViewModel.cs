using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StockManufactura.Application.Interfaces;
using StockManufactura.Application.Resources;
using StockManufactura.Desktop.Infrastructure;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Desktop.ViewModels
{
    public sealed partial class SpecialResourcesViewModel : ObservableObject
    {
        private readonly IResourcePricingService _resourcePricingService;

        [ObservableProperty]
        private Recurso? _selectedResource;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _isNewResourcePanelVisible;

        [ObservableProperty]
        private string _nuevoCodigo = string.Empty;

        [ObservableProperty]
        private string _nuevoNombre = string.Empty;

        [ObservableProperty]
        private string _nuevaUnidadMedida = string.Empty;

        [ObservableProperty]
        private string _nuevaCategoria = string.Empty;

        public SpecialResourcesViewModel(IResourcePricingService resourcePricingService)
        {
            _resourcePricingService = resourcePricingService ?? throw new ArgumentNullException(nameof(resourcePricingService));

            SpecialResources = new ObservableCollection<Recurso>();
            Tramos = new ObservableCollection<TramoRowViewModel>();

            LoadCommand = new AsyncRelayCommand(LoadAsync);
            ToggleNewResourcePanelCommand = new RelayCommand(ToggleNewResourcePanel);
            CreateResourceCommand = new AsyncRelayCommand(CreateResourceAsync);
            AddTramoCommand = new RelayCommand(AddTramo);
            RemoveTramoCommand = new RelayCommand<TramoRowViewModel>(RemoveTramo);
            SaveTramosCommand = new AsyncRelayCommand(SaveTramosAsync);

            _ = LoadAsync();
        }

        public ObservableCollection<Recurso> SpecialResources { get; }
        public ObservableCollection<TramoRowViewModel> Tramos { get; }

        public ICommand LoadCommand { get; }
        public ICommand ToggleNewResourcePanelCommand { get; }
        public ICommand CreateResourceCommand { get; }
        public ICommand AddTramoCommand { get; }
        public ICommand RemoveTramoCommand { get; }
        public ICommand SaveTramosCommand { get; }

        partial void OnSelectedResourceChanged(Recurso? value)
        {
            _ = LoadTramosAsync(value);
        }

        private async Task LoadAsync()
        {
            try
            {
                var resources = await _resourcePricingService.GetResourcesAsync();
                var previouslySelectedId = SelectedResource?.Id;

                SpecialResources.Clear();
                foreach (var resource in resources.Where(r => r.EsServicio).OrderBy(r => r.Nombre))
                {
                    SpecialResources.Add(resource);
                }

                SelectedResource = SpecialResources.FirstOrDefault(x => x.Id == previouslySelectedId)
                    ?? SpecialResources.FirstOrDefault();

                StatusMessage = "Insumos especiales cargados.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error al cargar insumos especiales: {ex.Message}";
            }
        }

        private async Task LoadTramosAsync(Recurso? resource)
        {
            Tramos.Clear();
            if (resource is null)
            {
                return;
            }

            try
            {
                var tramos = await _resourcePricingService.GetTramosAsync(resource.Id);
                foreach (var tramo in tramos)
                {
                    Tramos.Add(new TramoRowViewModel(
                        tramo.CantidadHasta?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                        tramo.PrecioPorUnidad.ToString(CultureInfo.InvariantCulture)));
                }

                if (Tramos.Count == 0)
                {
                    AddTramo();
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error al cargar tramos: {ex.Message}";
            }
        }

        private void ToggleNewResourcePanel()
        {
            IsNewResourcePanelVisible = !IsNewResourcePanelVisible;
            NuevoCodigo = string.Empty;
            NuevoNombre = string.Empty;
            NuevaUnidadMedida = string.Empty;
            NuevaCategoria = string.Empty;
        }

        private async Task CreateResourceAsync()
        {
            if (string.IsNullOrWhiteSpace(NuevoCodigo) || string.IsNullOrWhiteSpace(NuevoNombre))
            {
                StatusMessage = "Código y nombre son requeridos.";
                return;
            }

            try
            {
                var actor = AuthSession.Current?.Usuario.Email ?? "desktop-user";
                var request = new ResourceUpsertRequest
                {
                    Codigo = NuevoCodigo.Trim(),
                    Nombre = NuevoNombre.Trim(),
                    Descripcion = string.Empty,
                    Categoria = NuevaCategoria.Trim(),
                    UnidadMedida = NuevaUnidadMedida.Trim(),
                    EsServicio = true,
                    StockActual = 0,
                    StockMinimo = 0,
                    Precio = 0,
                    Moneda = Moneda.ARS,
                    Observaciones = string.Empty,
                    Activo = true,
                    MotivoCambio = "Alta de insumo especial",
                    Usuario = actor
                };

                var created = await _resourcePricingService.UpsertResourceAsync(request);
                IsNewResourcePanelVisible = false;
                await LoadAsync();
                SelectedResource = SpecialResources.FirstOrDefault(x => x.Id == created.Id);
                StatusMessage = "Insumo especial creado. Ahora definí sus tramos de precio.";
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
            }
        }

        private void AddTramo()
        {
            Tramos.Add(new TramoRowViewModel(string.Empty, "0"));
        }

        private void RemoveTramo(TramoRowViewModel? row)
        {
            if (row is null)
            {
                return;
            }

            Tramos.Remove(row);
        }

        private async Task SaveTramosAsync()
        {
            if (SelectedResource is null)
            {
                StatusMessage = "Seleccioná un insumo especial.";
                return;
            }

            if (Tramos.Count == 0)
            {
                StatusMessage = "Debe definir al menos un tramo de precio.";
                return;
            }

            var tramosInput = new List<TramoPrecioInput>();
            foreach (var row in Tramos)
            {
                if (!decimal.TryParse(row.PrecioPorUnidad, NumberStyles.Number, CultureInfo.InvariantCulture, out var precio) || precio < 0)
                {
                    StatusMessage = "Hay un precio de tramo inválido.";
                    return;
                }

                decimal? cantidadHasta = null;
                if (!string.IsNullOrWhiteSpace(row.CantidadHasta))
                {
                    if (!decimal.TryParse(row.CantidadHasta, NumberStyles.Number, CultureInfo.InvariantCulture, out var cantidad) || cantidad <= 0)
                    {
                        StatusMessage = "Hay una cantidad de tramo inválida.";
                        return;
                    }

                    cantidadHasta = cantidad;
                }

                tramosInput.Add(new TramoPrecioInput { CantidadHasta = cantidadHasta, PrecioPorUnidad = precio });
            }

            try
            {
                var actor = AuthSession.Current?.Usuario.Email ?? "desktop-user";
                await _resourcePricingService.GuardarTramosAsync(SelectedResource.Id, tramosInput, actor);
                StatusMessage = "Tramos guardados. El costo de los productos que usan este insumo ya se recalculó.";
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
            }
        }
    }

    public sealed partial class TramoRowViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _cantidadHasta;

        [ObservableProperty]
        private string _precioPorUnidad;

        public TramoRowViewModel(string cantidadHasta, string precioPorUnidad)
        {
            _cantidadHasta = cantidadHasta;
            _precioPorUnidad = precioPorUnidad;
        }
    }
}
