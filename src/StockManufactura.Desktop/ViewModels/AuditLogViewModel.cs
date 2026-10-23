using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using StockManufactura.Application.Interfaces;
using StockManufactura.Application.Services;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Desktop.ViewModels
{
    public sealed partial class AuditLogViewModel : ObservableObject
    {
        private readonly IAuditLogService _auditLogService;
        private readonly ISystemStatusService _systemStatusService;

        [ObservableProperty]
        private DateTime? _fromDate;

        [ObservableProperty]
        private DateTime? _toDate;

        [ObservableProperty]
        private string _usuario = string.Empty;

        [ObservableProperty]
        private string _modulo = string.Empty;

        [ObservableProperty]
        private string _accion = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(AuditEventCountText), nameof(AuditUserCountText), nameof(TraceabilityCoverageText),
            nameof(TraceabilityChangesText), nameof(CloudSyncStatusText), nameof(LastDriveSyncText), nameof(LastAuditText))]
        private SystemStatusSnapshot? _status;

        public AuditLogViewModel(IAuditLogService auditLogService, ISystemStatusService systemStatusService)
        {
            _auditLogService = auditLogService;
            _systemStatusService = systemStatusService;
            Items = new ObservableCollection<AuditLog>();
            SearchCommand = new AsyncRelayCommand(SearchAsync);
            _ = SearchAsync();
            _ = LoadSummaryAsync();
        }

        public ObservableCollection<AuditLog> Items { get; }
        public ICommand SearchCommand { get; }

        // Indicadores de trazabilidad y auditoría del mes en curso (antes ubicados en el panel principal).
        public string AuditEventCountText => Status?.AuditEventCount.ToString() ?? "0";
        public string AuditUserCountText => Status is null ? "Sin actividad" : $"{Status.AuditUserCount} usuario(s) distintos";
        public string TraceabilityCoverageText => Status is null ? "0 %" : $"{Status.TraceabilityCoveragePercent:0.#} %";
        public string TraceabilityChangesText => Status is null ? "0" : (Status.PriceChangeCount + Status.CostRecalculationCount).ToString();
        public string CloudSyncStatusText => Status?.CloudSyncStatus ?? "No configurado";
        public string LastDriveSyncText => Status?.LastDriveSyncAt is DateTime syncAt
            ? $"Última: {syncAt.ToLocalTime():dd/MM/yyyy HH:mm}"
            : "Sin sincronización";
        public string LastAuditText => Status?.LastAuditAt is DateTime auditAt
            ? $"Último evento: {auditAt.ToLocalTime():dd/MM/yyyy HH:mm} · {Status!.LastAuditDescription}"
            : "Sin eventos registrados";

        private async Task SearchAsync()
        {
            // El selector entrega la fecha a las 00:00: "Hasta" incluye el día completo.
            var hasta = ToDate?.Date.AddDays(1).AddTicks(-1);
            var logs = await _auditLogService.QueryAsync(FromDate?.Date, hasta, Usuario, Modulo, Accion);
            Items.Clear();
            foreach (var log in logs)
            {
                Items.Add(log);
            }
        }

        private async Task LoadSummaryAsync()
        {
            try
            {
                Status = await _systemStatusService.GetSnapshotAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "No se pudo cargar el resumen de trazabilidad");
            }
        }
    }
}
