using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StockManufactura.Application.Interfaces;
using StockManufactura.Application.Products;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Desktop.ViewModels
{
    public sealed partial class ProductCostHistoryViewModel : ObservableObject
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IProductCostService _productCostService;

        [ObservableProperty]
        private Producto? _selectedProduct;

        [ObservableProperty]
        private ProductCostHistory? _selectedOlder;

        [ObservableProperty]
        private ProductCostHistory? _selectedNewer;

        [ObservableProperty]
        private string _comparisonSummary = "Sin comparación";

        public ProductCostHistoryViewModel(IUnitOfWork unitOfWork, IProductCostService productCostService)
        {
            _unitOfWork = unitOfWork;
            _productCostService = productCostService;
            Products = new ObservableCollection<Producto>();
            History = new ObservableCollection<ProductCostHistory>();
            CompareCommand = new AsyncRelayCommand(CompareAsync);
            LoadHistoryCommand = new AsyncRelayCommand(LoadHistoryAsync);
            _ = LoadProductsAsync();
        }

        public ObservableCollection<Producto> Products { get; }
        public ObservableCollection<ProductCostHistory> History { get; }

        public ICommand CompareCommand { get; }
        public ICommand LoadHistoryCommand { get; }

        partial void OnSelectedProductChanged(Producto? value)
        {
            _ = LoadHistoryAsync();
        }

        private async Task LoadProductsAsync()
        {
            var products = await _unitOfWork.Productos.ListActivosAsync();
            Products.Clear();
            foreach (var product in products)
            {
                Products.Add(product);
            }

            SelectedProduct = Products.FirstOrDefault();
        }

        private async Task LoadHistoryAsync()
        {
            if (SelectedProduct is null)
            {
                return;
            }

            var history = await _productCostService.GetProductCostHistoryAsync(SelectedProduct.Id);
            History.Clear();
            foreach (var item in history)
            {
                History.Add(item);
            }
        }

        private async Task CompareAsync()
        {
            if (SelectedOlder is null || SelectedNewer is null)
            {
                ComparisonSummary = "Seleccioná dos versiones para comparar.";
                return;
            }

            ProductCostComparison comparison = await _productCostService.CompareVersionsAsync(SelectedOlder.Id, SelectedNewer.Id);
            var recursos = comparison.RecursosModificados.Count == 0
                ? "sin cambios detectados"
                : string.Join(", ", comparison.RecursosModificados);

            ComparisonSummary = $"Anterior: {comparison.CostoAnterior:0.0000} | Nuevo: {comparison.CostoNuevo:0.0000} | Variación: {comparison.VariacionAbsoluta:0.0000} ({comparison.VariacionPorcentual:P2}) | Cotización: {comparison.CotizacionUtilizada:0.0000} | Recursos: {recursos}";
        }
    }
}
