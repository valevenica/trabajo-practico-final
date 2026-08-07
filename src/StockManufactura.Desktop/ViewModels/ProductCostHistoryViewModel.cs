using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StockManufactura.Application.Interfaces;
using StockManufactura.Application.Products;
using StockManufactura.Desktop.Services;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Desktop.ViewModels
{
    public sealed partial class ProductCostHistoryViewModel : ObservableObject
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IProductCostService _productCostService;
        private readonly NavigationService? _navigationService;
        private readonly DashboardViewModel? _dashboardViewModel;
        private readonly ProductManagementViewModel? _returnTarget;
        private readonly Guid? _initialProductId;

        [ObservableProperty]
        private Producto? _selectedProduct;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        public ProductCostHistoryViewModel(
            IUnitOfWork unitOfWork,
            IProductCostService productCostService,
            NavigationService? navigationService = null,
            DashboardViewModel? dashboardViewModel = null,
            Producto? initialProduct = null,
            ProductManagementViewModel? returnTarget = null)
        {
            _unitOfWork = unitOfWork;
            _productCostService = productCostService;
            _navigationService = navigationService;
            _dashboardViewModel = dashboardViewModel;
            _returnTarget = returnTarget;
            _initialProductId = initialProduct?.Id;
            Products = new ObservableCollection<Producto>();
            Timeline = new ObservableCollection<ProductHistoryRow>();
            LoadHistoryCommand = new AsyncRelayCommand(LoadHistoryAsync);
            BackCommand = new RelayCommand(GoBack);
            _ = LoadProductsAsync();
        }

        public ObservableCollection<Producto> Products { get; }
        public ObservableCollection<ProductHistoryRow> Timeline { get; }

        public ICommand LoadHistoryCommand { get; }
        public ICommand BackCommand { get; }

        partial void OnSelectedProductChanged(Producto? value)
        {
            _ = LoadHistoryAsync();
        }

        private async Task LoadProductsAsync()
        {
            var products = await _unitOfWork.Productos.ListAsync();
            Products.Clear();
            foreach (var product in products.OrderBy(x => x.Nombre))
            {
                Products.Add(product);
            }

            SelectedProduct = _initialProductId.HasValue
                ? Products.FirstOrDefault(x => x.Id == _initialProductId.Value) ?? Products.FirstOrDefault()
                : Products.FirstOrDefault();
        }

        private async Task LoadHistoryAsync()
        {
            Timeline.Clear();
            if (SelectedProduct is null)
            {
                return;
            }

            var rows = new System.Collections.Generic.List<ProductHistoryRow>();

            // Cambios de costo
            var costHistory = await _productCostService.GetProductCostHistoryAsync(SelectedProduct.Id);
            foreach (var h in costHistory)
            {
                rows.Add(new ProductHistoryRow(
                    h.Fecha,
                    TipoEvento(h.MotivoRecalculo),
                    $"{h.MotivoRecalculo} | Costo: {h.CostoAnterior:0.00} → {h.CostoNuevo:0.00} | Precio: {h.PrecioSugeridoAnterior:0.00} → {h.PrecioSugeridoNuevo:0.00}",
                    h.Usuario,
                    h.CostoNuevo.ToString("0.00", CultureInfo.InvariantCulture)));
            }

            // Eventos de auditoría (productos + BOM)
            var auditEvents = await _unitOfWork.AuditLogs.ListByProductIdAsync(SelectedProduct.Id);
            foreach (var a in auditEvents)
            {
                rows.Add(new ProductHistoryRow(
                    a.FechaHora,
                    MapAuditAccion(a.Modulo, a.Accion),
                    a.Descripcion,
                    a.Usuario,
                    string.Empty));
            }

            foreach (var row in rows.OrderByDescending(x => x.FechaUtc))
            {
                Timeline.Add(row);
            }

            StatusMessage = Timeline.Count == 0 ? "Sin historial para este producto." : $"{Timeline.Count} evento(s) encontrados.";
        }

        private static string TipoEvento(string motivo) =>
            motivo.Contains("receta", StringComparison.OrdinalIgnoreCase) || motivo.Contains("BOM", StringComparison.OrdinalIgnoreCase)
                ? "Recalculo (BOM)"
                : "Recalculo de costo";

        private static string MapAuditAccion(string modulo, string accion) => accion switch
        {
            "CrearItem"       => "Insumo agregado",
            "EditarItem"      => "Insumo modificado",
            "EliminarItem"    => "Insumo eliminado",
            "RecalculoCostos" => "Recalculo (BOM)",
            "Crear"           => "Producto creado",
            "Editar"          => "Producto editado",
            _                 => $"{modulo}: {accion}"
        };

        private void GoBack()
        {
            if (_navigationService is null)
            {
                return;
            }

            if (_returnTarget is not null)
            {
                _navigationService.NavigateTo(_returnTarget);
            }
            else if (_dashboardViewModel is not null)
            {
                _navigationService.NavigateTo(_dashboardViewModel);
            }
        }
    }

    public sealed record ProductHistoryRow(
        DateTime FechaUtc,
        string TipoEvento,
        string Descripcion,
        string Usuario,
        string CostoNuevo)
    {
        public string Fecha => FechaUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm", CultureInfo.CurrentCulture);
    }
}
