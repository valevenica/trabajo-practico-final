using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StockManufactura.Application.Interfaces;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Desktop.ViewModels
{
    public sealed partial class AuditLogViewModel : ObservableObject
    {
        private readonly IAuditLogService _auditLogService;

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

        public AuditLogViewModel(IAuditLogService auditLogService)
        {
            _auditLogService = auditLogService;
            Items = new ObservableCollection<AuditLog>();
            SearchCommand = new AsyncRelayCommand(SearchAsync);
            _ = SearchAsync();
        }

        public ObservableCollection<AuditLog> Items { get; }
        public ICommand SearchCommand { get; }

        private async Task SearchAsync()
        {
            var logs = await _auditLogService.QueryAsync(FromDate, ToDate, Usuario, Modulo, Accion);
            Items.Clear();
            foreach (var log in logs)
            {
                Items.Add(log);
            }
        }
    }
}
