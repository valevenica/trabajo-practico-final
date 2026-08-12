using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using StockManufactura.Application.Interfaces;
using StockManufactura.Desktop.Infrastructure;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Desktop.ViewModels
{
    public sealed partial class BackupManagementViewModel : ObservableObject
    {
        private const string ResetConfirmationPhrase = "BORRAR TODO";

        private readonly IBackupService _backupService;
        private readonly DatabaseResetService _databaseResetService;

        [ObservableProperty]
        private string _folder = string.Empty;

        [ObservableProperty]
        private bool _automatic;

        [ObservableProperty]
        private int _keepCopies = 10;

        [ObservableProperty]
        private int _intervalMinutes = 60;

        [ObservableProperty]
        private string _statusMessage = "Listo";

        [ObservableProperty]
        private string _restoreFilePath = string.Empty;

        [ObservableProperty]
        private string _resetConfirmationText = string.Empty;

        public BackupManagementViewModel(IBackupService backupService)
        {
            _backupService = backupService;
            _databaseResetService = new DatabaseResetService();
            Records = new ObservableCollection<BackupRecord>();
            SaveSettingsCommand = new AsyncRelayCommand(SaveSettingsAsync);
            ManualBackupCommand = new AsyncRelayCommand(ManualBackupAsync);
            RestoreBackupCommand = new AsyncRelayCommand(RestoreBackupAsync);
            RefreshCommand = new AsyncRelayCommand(LoadAsync);
            ResetDatabaseCommand = new AsyncRelayCommand(ResetDatabaseAsync);
            _ = LoadAsync();
        }

        public ObservableCollection<BackupRecord> Records { get; }

        public ICommand SaveSettingsCommand { get; }
        public ICommand ManualBackupCommand { get; }
        public ICommand RestoreBackupCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ResetDatabaseCommand { get; }

        public bool CanResetDatabase => AuthSession.Current?.TienePermiso("USUARIOS_ADMIN") == true;

        private async Task LoadAsync()
        {
            var settings = await _backupService.GetSettingsAsync();
            Folder = settings.CarpetaLocal;
            Automatic = settings.Automatico;
            KeepCopies = settings.MantenerUltimasCopias;
            IntervalMinutes = settings.IntervaloMinutos;

            var records = await _backupService.GetRecentBackupsAsync();
            Records.Clear();
            foreach (var record in records)
            {
                Records.Add(record);
            }
        }

        private async Task SaveSettingsAsync()
        {
            var settings = await _backupService.GetSettingsAsync();
            settings.CarpetaLocal = Folder;
            settings.Automatico = Automatic;
            settings.MantenerUltimasCopias = KeepCopies;
            settings.IntervaloMinutos = IntervalMinutes;
            await _backupService.SaveSettingsAsync(settings);
            StatusMessage = "Configuracion guardada";
        }

        private async Task ManualBackupAsync()
        {
            var record = await _backupService.CreateManualBackupAsync("desktop-user");
            Records.Insert(0, record);
            StatusMessage = "Backup manual completado";
        }

        private async Task RestoreBackupAsync()
        {
            if (string.IsNullOrWhiteSpace(RestoreFilePath))
            {
                StatusMessage = "Indicá el archivo ZIP a restaurar.";
                return;
            }

            var record = await _backupService.RestoreBackupAsync(RestoreFilePath, "desktop-user");
            Records.Insert(0, record);
            StatusMessage = "Restauración completada";
        }

        private async Task ResetDatabaseAsync()
        {
            if (!CanResetDatabase)
            {
                StatusMessage = "No tiene permisos para restablecer la base de datos.";
                return;
            }

            if (!string.Equals(ResetConfirmationText.Trim(), ResetConfirmationPhrase, StringComparison.Ordinal))
            {
                StatusMessage = $"Escribí exactamente \"{ResetConfirmationPhrase}\" para confirmar.";
                return;
            }

            var confirm = MessageBox.Show(
                "Esto va a borrar TODOS los datos de la aplicación (productos, insumos, proveedores, órdenes de producción, historial de costos y precios, auditoría, backups registrados) de forma irreversible. Solo quedarán los roles y el usuario administrador base.\n\n" +
                "Se va a generar un backup automático antes de borrar, por las dudas.\n\n¿Confirmás que querés continuar?",
                "Restablecer base de datos",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);
            if (confirm != MessageBoxResult.Yes)
            {
                StatusMessage = "Restablecimiento cancelado.";
                return;
            }

            var actor = AuthSession.Current?.Usuario.Email ?? "desktop-user";

            try
            {
                StatusMessage = "Generando backup de seguridad...";
                await _backupService.CreateManualBackupAsync(actor);

                StatusMessage = "Restableciendo base de datos...";
                _databaseResetService.ResetToBlank(actor);

                ResetConfirmationText = string.Empty;

                MessageBox.Show(
                    "La base de datos fue restablecida. La aplicación se va a reiniciar para cargar el estado limpio.",
                    "Listo",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                var exePath = Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(exePath))
                {
                    Process.Start(exePath);
                }

                System.Windows.Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ResetDatabaseAsync failed");
                StatusMessage = $"Error al restablecer la base de datos: {ex.Message}";
            }
        }
    }
}
