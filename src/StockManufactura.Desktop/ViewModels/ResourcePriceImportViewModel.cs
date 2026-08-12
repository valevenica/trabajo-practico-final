using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using StockManufactura.Application.Interfaces;
using StockManufactura.Application.Resources;
using StockManufactura.Desktop.Infrastructure;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Desktop.ViewModels
{
    public sealed partial class ResourcePriceImportViewModel : ObservableObject
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IResourcePriceImportService _importService;
        private string? _archivoPath;

        [ObservableProperty]
        private Proveedor? _selectedProveedor;

        [ObservableProperty]
        private string _archivoSeleccionado = "Ningún archivo seleccionado.";

        [ObservableProperty]
        private string _statusMessage = "Elegí un proveedor y un archivo Excel (.xlsx) con columnas: Código de proveedor | Precio.";

        [ObservableProperty]
        private bool _isBusy;

        public ResourcePriceImportViewModel(IUnitOfWork unitOfWork, IResourcePriceImportService importService)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _importService = importService ?? throw new ArgumentNullException(nameof(importService));

            Proveedores = new ObservableCollection<Proveedor>();
            Filas = new ObservableCollection<ResourcePriceImportRow>();

            LoadCommand = new AsyncRelayCommand(LoadAsync);
            SeleccionarArchivoCommand = new AsyncRelayCommand(SeleccionarArchivoAsync);
            ConfirmarImportacionCommand = new AsyncRelayCommand(ConfirmarImportacionAsync, PuedeConfirmar);

            _ = LoadAsync();
        }

        public ObservableCollection<Proveedor> Proveedores { get; }
        public ObservableCollection<ResourcePriceImportRow> Filas { get; }

        public ICommand LoadCommand { get; }
        public ICommand SeleccionarArchivoCommand { get; }
        public IAsyncRelayCommand ConfirmarImportacionCommand { get; }

        public int CantidadOk => Filas.Count(x => x.Status == ResourcePriceImportRowStatus.Ok);

        partial void OnIsBusyChanged(bool value)
        {
            ConfirmarImportacionCommand.NotifyCanExecuteChanged();
        }

        partial void OnSelectedProveedorChanged(Proveedor? value)
        {
            Filas.Clear();
            OnPropertyChanged(nameof(CantidadOk));
            ConfirmarImportacionCommand.NotifyCanExecuteChanged();
            if (value is not null && _archivoPath is not null)
            {
                _ = ParsearAsync();
            }
        }

        private async Task LoadAsync()
        {
            try
            {
                var proveedores = await _unitOfWork.Proveedores.ListAsync();

                Proveedores.Clear();
                foreach (var proveedor in proveedores.Where(p => p.Activo).OrderBy(p => p.Nombre))
                {
                    Proveedores.Add(proveedor);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error al cargar proveedores: {ex.Message}";
            }
        }

        private async Task SeleccionarArchivoAsync()
        {
            if (SelectedProveedor is null)
            {
                StatusMessage = "Elegí primero un proveedor.";
                return;
            }

            var dialog = new OpenFileDialog
            {
                Filter = "Excel (*.xlsx)|*.xlsx",
                Title = "Seleccionar planilla de precios"
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            _archivoPath = dialog.FileName;
            ArchivoSeleccionado = Path.GetFileName(dialog.FileName);
            await ParsearAsync();
        }

        private async Task ParsearAsync()
        {
            if (SelectedProveedor is null || _archivoPath is null)
            {
                return;
            }

            try
            {
                IsBusy = true;
                StatusMessage = "Leyendo archivo...";

                using var stream = File.OpenRead(_archivoPath);
                var filas = await _importService.ParsearAsync(stream, SelectedProveedor.Id);

                Filas.Clear();
                foreach (var fila in filas)
                {
                    Filas.Add(fila);
                }

                OnPropertyChanged(nameof(CantidadOk));
                ConfirmarImportacionCommand.NotifyCanExecuteChanged();

                StatusMessage = Filas.Count == 0
                    ? "El archivo no tiene filas con datos."
                    : $"{Filas.Count} fila(s) leídas. {CantidadOk} con precio nuevo listas para importar.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error al leer el archivo: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool PuedeConfirmar() => !IsBusy && CantidadOk > 0;

        private async Task ConfirmarImportacionAsync()
        {
            var confirm = System.Windows.MessageBox.Show(
                $"Se van a actualizar {CantidadOk} insumo(s) con el nuevo precio. ¿Confirmar?",
                "Confirmar importación", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
            if (confirm != System.Windows.MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                IsBusy = true;
                var actor = AuthSession.Current?.Usuario.Email ?? "desktop-user";
                var result = await _importService.AplicarAsync(Filas, actor);

                StatusMessage = $"Importación completada: {result.Actualizados} insumo(s) actualizados, {result.Omitidos} omitido(s).";
                await ParsearAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error al importar: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
