using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
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
        private string _manualFuenteInput = string.Empty;

        [ObservableProperty]
        private string _selectedProviderKey = string.Empty;

        [ObservableProperty]
        private string _statusMessage = "Listo.";

        [ObservableProperty]
        private ExchangeRate? _selectedHistoryRate;

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
            SetPrioritariaCommand = new AsyncRelayCommand(SetPrioritariaAsync);

            _ = LoadAsync();
        }

        public ObservableCollection<IExchangeRateProvider> Providers { get; }
        public ObservableCollection<ExchangeRate> History { get; }

        public ICommand LoadCommand { get; }
        public ICommand ManualUpdateCommand { get; }
        public ICommand AutomaticUpdateCommand { get; }
        public ICommand RefreshHistoryCommand { get; }
        public ICommand SetPrioritariaCommand { get; }

        public async Task LoadAsync()
        {
            var state = await _service.GetCurrentStateAsync();
            CurrentRate = state.CurrentRate.ToString("0.00", CultureInfo.InvariantCulture);
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

            var fuente = string.IsNullOrWhiteSpace(ManualFuenteInput) ? "Manual" : ManualFuenteInput.Trim();
            try
            {
                var rate = await _service.UpdateManualAsync(value, fuente, "desktop-user");
                CurrentRate = rate.Valor.ToString("0.00", CultureInfo.InvariantCulture);
                LastUpdate = rate.Fecha.ToLocalTime().ToString("g");
                Source = rate.Fuente;
                StatusMessage = $"Cotizaci\u00f3n manual guardada: ${value:0.00} ({fuente})";
                await RefreshHistoryAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ManualUpdateAsync failed");
                StatusMessage = $"Error al guardar la cotizaci\u00f3n manual: {ex.Message}";
            }
        }

        private async Task AutomaticUpdateAsync()
        {
            StatusMessage = "Buscando cotización dólar blue...";
            try
            {
                // Always fetch dolar blue from DolarHoy
                var rate = await _service.UpdateAutomaticAsync("dolar-hoy", "desktop-user");
                CurrentRate = rate.Valor.ToString("0.00", CultureInfo.InvariantCulture);
                LastUpdate = rate.Fecha.ToLocalTime().ToString("g");
                Source = rate.Fuente;
                StatusMessage = $"D\u00f3lar blue actualizado: ${rate.Valor:0.00}";
                await RefreshHistoryAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "AutomaticUpdateAsync failed");
                var inner = ex.InnerException?.Message ?? string.Empty;
                StatusMessage = $"Error: {ex.Message}{(string.IsNullOrEmpty(inner) ? "" : " | " + inner)}";
            }
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

        private async Task SetPrioritariaAsync()
        {
            if (SelectedHistoryRate is null)
            {
                StatusMessage = "Seleccioná una cotización de la tabla.";
                return;
            }
            try
            {
                await _service.SetPrioritariaAsync(SelectedHistoryRate.Id, "desktop-user");
                StatusMessage = $"Cotización prioritaria: ${SelectedHistoryRate.Valor:0.00} ({SelectedHistoryRate.Fuente})";
                await LoadAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "SetPrioritariaAsync failed");
                StatusMessage = $"Error: {ex.Message}";
            }
        }
    }
}
