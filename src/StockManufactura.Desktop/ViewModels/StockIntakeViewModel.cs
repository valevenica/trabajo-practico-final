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

namespace StockManufactura.Desktop.ViewModels
{
    public sealed partial class StockIntakeViewModel : ObservableObject
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStockIntakeService _stockIntakeService;

        [ObservableProperty]
        private StockItemOption? _selectedItem;

        [ObservableProperty]
        private string _cantidad = string.Empty;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        public StockIntakeViewModel(IUnitOfWork unitOfWork, IStockIntakeService stockIntakeService)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _stockIntakeService = stockIntakeService ?? throw new ArgumentNullException(nameof(stockIntakeService));

            AvailableItems = new ObservableCollection<StockItemOption>();
            Lines = new ObservableCollection<StockIntakeLineRow>();

            LoadCommand = new AsyncRelayCommand(LoadAsync);
            AddLineCommand = new RelayCommand(AddLine);
            RemoveLineCommand = new RelayCommand<StockIntakeLineRow>(RemoveLine);
            SaveCommand = new AsyncRelayCommand(SaveAsync);

            _ = LoadAsync();
        }

        public ObservableCollection<StockItemOption> AvailableItems { get; }
        public ObservableCollection<StockIntakeLineRow> Lines { get; }

        public ICommand LoadCommand { get; }
        public ICommand AddLineCommand { get; }
        public ICommand RemoveLineCommand { get; }
        public ICommand SaveCommand { get; }

        private async Task LoadAsync()
        {
            try
            {
                var productos = await _unitOfWork.Productos.ListActivosAsync();
                var recursos = await _unitOfWork.Recursos.ListActivosAsync();

                AvailableItems.Clear();
                foreach (var recurso in recursos.Where(r => !r.EsServicio).OrderBy(r => r.Nombre))
                {
                    AvailableItems.Add(new StockItemOption(recurso.Id, recurso.Nombre, false, recurso.UnidadMedida));
                }
                foreach (var producto in productos.OrderBy(p => p.Nombre))
                {
                    AvailableItems.Add(new StockItemOption(producto.Id, producto.Nombre, true, "u."));
                }

                StatusMessage = "Listo para registrar ingresos.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error al cargar productos e insumos: {ex.Message}";
            }
        }

        private void AddLine()
        {
            if (SelectedItem is null)
            {
                StatusMessage = "Seleccioná un producto o insumo.";
                return;
            }

            if (!decimal.TryParse(Cantidad, NumberStyles.Number, CultureInfo.InvariantCulture, out var cantidad) || cantidad <= 0)
            {
                StatusMessage = "Ingresá una cantidad válida.";
                return;
            }

            var existing = Lines.FirstOrDefault(x => x.ItemId == SelectedItem.Id);
            if (existing is not null)
            {
                var index = Lines.IndexOf(existing);
                Lines[index] = existing with { Cantidad = existing.Cantidad + cantidad };
            }
            else
            {
                Lines.Add(new StockIntakeLineRow(SelectedItem.Id, SelectedItem.Nombre, SelectedItem.EsProducto, SelectedItem.Unidad, cantidad));
            }

            SelectedItem = null;
            Cantidad = string.Empty;
            StatusMessage = "Ítem agregado a la lista. Guardá para aplicar los ingresos.";
        }

        private void RemoveLine(StockIntakeLineRow? line)
        {
            if (line is null)
            {
                return;
            }

            Lines.Remove(line);
        }

        private async Task SaveAsync()
        {
            if (Lines.Count == 0)
            {
                StatusMessage = "Agregá al menos un ítem antes de guardar.";
                return;
            }

            try
            {
                var actor = AuthSession.Current?.Usuario.Email ?? "desktop-user";
                var lineas = Lines
                    .Select(l => new StockIntakeLineInput { ItemId = l.ItemId, EsProducto = l.EsProducto, Cantidad = l.Cantidad })
                    .ToList();

                await _stockIntakeService.RegistrarIngresoAsync(lineas, actor);

                Lines.Clear();
                StatusMessage = "Ingreso de stock registrado correctamente.";
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
            }
        }
    }

    public sealed record StockItemOption(Guid Id, string Nombre, bool EsProducto, string Unidad);

    public sealed record StockIntakeLineRow(Guid ItemId, string Nombre, bool EsProducto, string Unidad, decimal Cantidad)
    {
        public string TipoLabel => EsProducto ? "Producto" : "Insumo";
    }
}
