using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StockManufactura.Application.Interfaces;
using StockManufactura.Application.Resources;
using StockManufactura.Desktop.Services;

namespace StockManufactura.Desktop.ViewModels
{
    public sealed partial class ShortagesViewModel : ObservableObject
    {
        private readonly IShortageService _shortageService;
        private readonly NavigationService _navigationService;
        private readonly DashboardViewModel _dashboardViewModel;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        public ShortagesViewModel(
            IShortageService shortageService,
            NavigationService navigationService,
            DashboardViewModel dashboardViewModel)
        {
            _shortageService = shortageService ?? throw new ArgumentNullException(nameof(shortageService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _dashboardViewModel = dashboardViewModel ?? throw new ArgumentNullException(nameof(dashboardViewModel));

            Productos = new ObservableCollection<ProductShortageRow>();
            Insumos = new ObservableCollection<ResourceShortageRow>();

            LoadCommand = new AsyncRelayCommand(LoadAsync);
            BackCommand = new RelayCommand(GoBack);

            _ = LoadAsync();
        }

        public ObservableCollection<ProductShortageRow> Productos { get; }
        public ObservableCollection<ResourceShortageRow> Insumos { get; }

        public ICommand LoadCommand { get; }
        public ICommand BackCommand { get; }

        private async Task LoadAsync()
        {
            try
            {
                var report = await _shortageService.GetShortagesAsync();

                Productos.Clear();
                foreach (var row in report.Productos)
                {
                    Productos.Add(row);
                }

                Insumos.Clear();
                foreach (var row in report.Insumos)
                {
                    Insumos.Add(row);
                }

                StatusMessage = report.TotalCount == 0
                    ? "No hay productos ni insumos faltantes."
                    : $"{report.Productos.Count} producto(s) y {report.Insumos.Count} insumo(s) faltantes.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error al cargar faltantes: {ex.Message}";
            }
        }

        private void GoBack()
        {
            _navigationService.NavigateTo(_dashboardViewModel);
        }
    }
}
