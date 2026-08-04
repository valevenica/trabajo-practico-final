using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StockManufactura.Application.Interfaces;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Desktop.ViewModels
{
    public sealed partial class MonetaryConfigurationViewModel : ObservableObject
    {
        private readonly IMonetaryConfigurationService _service;

        [ObservableProperty]
        private string _currentRate = "0";

        [ObservableProperty]
        private string _lastUpdate = "-";

        [ObservableProperty]
        private string _source = "Sin cotización";

        [ObservableProperty]
        private string _manualRateInput = string.Empty;

        [ObservableProperty]
        private string _selectedProviderKey = string.Empty;

        [ObservableProperty]
        private string _statusMessage = "Listo.";

        public MonetaryConfigurationViewModel(IMonetaryConfigurationService service)
        {
            _service = service;
            Providers = new ObservableCollection<IExchangeRateProvider>(_service.GetProviders());
            SelectedProviderKey = Providers.FirstOrDefault()?.Key ?? string.Empty;
            History = new ObservableCollection<ExchangeRate>();

            LoadCommand = new AsyncRelayCommand(LoadAsync);
            ManualUpdateCommand = new AsyncRelayCommand(ManualUpdateAsync);
            AutomaticUpdateCommand = new AsyncRelayCommand(AutomaticUpdateAsync);
            RefreshHistoryCommand = new AsyncRelayCommand(RefreshHistoryAsync);

            _ = LoadAsync();
        }

        public ObservableCollection<IExchangeRateProvider> Providers { get; }
        public ObservableCollection<ExchangeRate> History { get; }

        public ICommand LoadCommand { get; }
        public ICommand ManualUpdateCommand { get; }
        public ICommand AutomaticUpdateCommand { get; }
        public ICommand RefreshHistoryCommand { get; }

        public async Task LoadAsync()
        {
            var state = await _service.GetCurrentStateAsync();
            CurrentRate = state.CurrentRate.ToString("0.0000", CultureInfo.InvariantCulture);
            LastUpdate = state.LastUpdate == DateTime.MinValue ? "-" : state.LastUpdate.ToLocalTime().ToString("g");
            Source = state.Source;
            await RefreshHistoryAsync();
        }

        private async Task ManualUpdateAsync()
        {
            if (!decimal.TryParse(ManualRateInput, NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
            {
                StatusMessage = "Ingresá una cotización válida (usar punto decimal).";
                return;
            }

            var rate = await _service.UpdateManualAsync(value, Source == "Sin cotización" ? "Manual" : Source, "desktop-user");
            CurrentRate = rate.Valor.ToString("0.0000", CultureInfo.InvariantCulture);
            LastUpdate = rate.Fecha.ToLocalTime().ToString("g");
            Source = rate.Fuente;
            StatusMessage = "Cotización actualizada manualmente.";
            await RefreshHistoryAsync();
        }

        private async Task AutomaticUpdateAsync()
        {
            if (string.IsNullOrWhiteSpace(SelectedProviderKey))
            {
                StatusMessage = "Seleccioná una fuente para actualización automática.";
                return;
            }

            var rate = await _service.UpdateAutomaticAsync(SelectedProviderKey, "desktop-user");
            CurrentRate = rate.Valor.ToString("0.0000", CultureInfo.InvariantCulture);
            LastUpdate = rate.Fecha.ToLocalTime().ToString("g");
            Source = rate.Fuente;
            StatusMessage = "Cotización actualizada automáticamente.";
            await RefreshHistoryAsync();
        }

        private async Task RefreshHistoryAsync()
        {
            var history = await _service.GetHistoryAsync();
            History.Clear();
            foreach (var rate in history)
            {
                History.Add(rate);
            }
        }
    }
}
