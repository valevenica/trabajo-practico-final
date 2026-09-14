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
using StockManufactura.Application.Reports;

namespace StockManufactura.Desktop.ViewModels
{
    /// <summary>
    /// Panel de inteligencia de negocio. Toda la agregacion vive en
    /// <see cref="IBusinessIntelligenceService"/>: este ViewModel solo expone el resultado
    /// y el estado de carga para la vista.
    /// </summary>
    public sealed partial class BusinessIntelligenceViewModel : ObservableObject
    {
        private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-AR");

        private readonly IBusinessIntelligenceService _businessIntelligenceService;

        [ObservableProperty] private BusinessIntelligenceReport? _report;
        [ObservableProperty] private PeriodOption? _selectedPeriod;
        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private string _statusMessage = string.Empty;

        public BusinessIntelligenceViewModel(IBusinessIntelligenceService businessIntelligenceService)
        {
            _businessIntelligenceService = businessIntelligenceService
                ?? throw new ArgumentNullException(nameof(businessIntelligenceService));

            PeriodOptions = new ObservableCollection<PeriodOption>(BuildPeriodOptions());
            CostoPorProducto = new ObservableCollection<ChartPoint>();
            MayoresAumentos = new ObservableCollection<ChartPoint>();
            OrdenesPorEstado = new ObservableCollection<ChartPoint>();
            EvolucionCosto = new ObservableCollection<ChartPoint>();
            ActividadPorModulo = new ObservableCollection<ChartPoint>();
            InsumosPorValorInmovilizado = new ObservableCollection<ChartPoint>();
            Alertas = new ObservableCollection<DecisionAlert>();

            RefreshCommand = new AsyncRelayCommand(LoadAsync);

            _selectedPeriod = PeriodOptions.FirstOrDefault(p => p.Year == DateTime.UtcNow.Year && p.Month == DateTime.UtcNow.Month)
                ?? PeriodOptions[0];

            _ = LoadAsync();
        }

        public ObservableCollection<PeriodOption> PeriodOptions { get; }
        public ObservableCollection<ChartPoint> CostoPorProducto { get; }
        public ObservableCollection<ChartPoint> MayoresAumentos { get; }
        public ObservableCollection<ChartPoint> OrdenesPorEstado { get; }
        public ObservableCollection<ChartPoint> EvolucionCosto { get; }
        public ObservableCollection<ChartPoint> ActividadPorModulo { get; }
        public ObservableCollection<ChartPoint> InsumosPorValorInmovilizado { get; }
        public ObservableCollection<DecisionAlert> Alertas { get; }

        public ICommand RefreshCommand { get; }

        public string PeriodoTexto => Report?.PeriodoDescripcion ?? "—";
        public string ProductosActivosTexto => Report?.ProductosActivos.ToString(Cultura) ?? "0";
        public string InsumosActivosTexto => Report?.InsumosActivos.ToString(Cultura) ?? "0";
        public string CostoPromedioTexto => Moneda(Report?.CostoPromedioProducto);
        public string MargenPromedioTexto => Report is null ? "—" : $"{(Report.MargenPromedio * 100m).ToString("N1", Cultura)} %";
        public string ValorInventarioInsumosTexto => Moneda(Report?.ValorInventarioInsumos);
        public string ValorInventarioProductosTexto => Moneda(Report?.ValorInventarioProductos);
        public string ProduccionValorizadaTexto => Moneda(Report?.ProduccionValorizada);
        public string OrdenesFinalizadasTexto => Report?.OrdenesFinalizadas.ToString(Cultura) ?? "0";
        public string OrdenesEnProcesoTexto => Report?.OrdenesEnProceso.ToString(Cultura) ?? "0";
        public string CotizacionTexto => Moneda(Report?.CotizacionVigente);

        public string VariacionCostoTexto => Report is null
            ? "—"
            : $"{(Report.VariacionCostoPromedioPct >= 0 ? "+" : string.Empty)}{Report.VariacionCostoPromedioPct.ToString("N2", Cultura)} %";

        public string VariacionCostoTono => Report is null
            ? "Neutral"
            : Report.VariacionCostoPromedioPct >= 10m ? "Critica"
            : Report.VariacionCostoPromedioPct > 0m ? "Advertencia"
            : "Ok";

        public bool HasData => Report is not null;

        partial void OnSelectedPeriodChanged(PeriodOption? value) => _ = LoadAsync();

        partial void OnReportChanged(BusinessIntelligenceReport? value)
        {
            Replace(CostoPorProducto, value?.CostoPorProducto);
            Replace(MayoresAumentos, value?.MayoresAumentos);
            Replace(OrdenesPorEstado, value?.OrdenesPorEstado);
            Replace(EvolucionCosto, value?.EvolucionCosto);
            Replace(ActividadPorModulo, value?.ActividadPorModulo);
            Replace(InsumosPorValorInmovilizado, value?.InsumosPorValorInmovilizado);
            Replace(Alertas, value?.Alertas);

            OnPropertyChanged(nameof(PeriodoTexto));
            OnPropertyChanged(nameof(ProductosActivosTexto));
            OnPropertyChanged(nameof(InsumosActivosTexto));
            OnPropertyChanged(nameof(CostoPromedioTexto));
            OnPropertyChanged(nameof(MargenPromedioTexto));
            OnPropertyChanged(nameof(ValorInventarioInsumosTexto));
            OnPropertyChanged(nameof(ValorInventarioProductosTexto));
            OnPropertyChanged(nameof(ProduccionValorizadaTexto));
            OnPropertyChanged(nameof(OrdenesFinalizadasTexto));
            OnPropertyChanged(nameof(OrdenesEnProcesoTexto));
            OnPropertyChanged(nameof(CotizacionTexto));
            OnPropertyChanged(nameof(VariacionCostoTexto));
            OnPropertyChanged(nameof(VariacionCostoTono));
            OnPropertyChanged(nameof(HasData));
        }

        private async Task LoadAsync()
        {
            if (IsLoading)
            {
                return;
            }

            try
            {
                IsLoading = true;
                StatusMessage = string.Empty;
                var period = SelectedPeriod;
                Report = await Task.Run(() => _businessIntelligenceService.GetReportAsync(period?.Year, period?.Month));
            }
            catch (Exception ex)
            {
                StatusMessage = $"No se pudo generar el tablero: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T>? source)
        {
            target.Clear();
            if (source is null)
            {
                return;
            }

            foreach (var item in source)
            {
                target.Add(item);
            }
        }

        private static string Moneda(decimal? valor) => valor is null ? "—" : valor.Value.ToString("C2", Cultura);

        private static IEnumerable<PeriodOption> BuildPeriodOptions()
        {
            yield return new PeriodOption(null, null, "Histórico completo");

            var cursor = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            for (var i = 0; i < 12; i++)
            {
                var label = cursor.ToString("MMMM yyyy", Cultura);
                label = char.ToUpperInvariant(label[0]) + label[1..];
                yield return new PeriodOption(cursor.Year, cursor.Month, label);
                cursor = cursor.AddMonths(-1);
            }
        }
    }
}
