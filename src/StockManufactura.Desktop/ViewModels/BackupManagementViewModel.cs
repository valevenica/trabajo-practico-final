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

        [ObservableProperty]
        private string _cloudFolder = string.Empty;

        [ObservableProperty]
        private string _cloudProvider = "Google Drive";

        [ObservableProperty]
        private bool _cloudEnabled;

        [ObservableProperty]
        private bool _cloudBidirectional = true;

        [ObservableProperty]
        private string _cloudSyncSummary = "Sin sincronizaciones registradas.";

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
            SynchronizeCloudCommand = new AsyncRelayCommand(SynchronizeCloudAsync);
            _ = LoadAsync();
        }

        public ObservableCollection<BackupRecord> Records { get; }

        public ICommand SaveSettingsCommand { get; }
        public ICommand ManualBackupCommand { get; }
        public ICommand RestoreBackupCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ResetDatabaseCommand { get; }
        public ICommand SynchronizeCloudCommand { get; }

        public bool CanResetDatabase => AuthSession.Current?.TienePermiso("USUARIOS_ADMIN") == true;

        private async Task LoadAsync()
        {
            var settings = await _backupService.GetSettingsAsync();
            Folder = settings.CarpetaLocal;
            Automatic = settings.Automatico;
            KeepCopies = settings.MantenerUltimasCopias;
            IntervalMinutes = settings.IntervaloMinutos;
            CloudFolder = settings.CarpetaNube;
            CloudProvider = settings.ProveedorNube;
            CloudEnabled = settings.GoogleDriveHabilitado;
            CloudBidirectional = settings.SincronizacionBidireccional;
            CloudSyncSummary = settings.UltimaSincronizacionNube is DateTime ultima
                ? $"Última sincronización: {ultima.ToLocalTime():dd/MM/yyyy HH:mm}."
                : "Sin sincronizaciones registradas.";

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
            settings.CarpetaNube = CloudFolder;
            settings.ProveedorNube = CloudProvider;
            settings.GoogleDriveHabilitado = CloudEnabled;
            settings.SincronizacionBidireccional = CloudBidirectional;
            await _backupService.SaveSettingsAsync(settings);
            StatusMessage = "Configuracion guardada";
        }

        private async Task SynchronizeCloudAsync()
        {
            var actor = AuthSession.Current?.Usuario.Email ?? "desktop-user";

            try
            {
                StatusMessage = "Sincronizando con la nube...";
                var result = await _backupService.SynchronizeCloudAsync(actor);

                if (!result.Ejecutada)
                {
                    StatusMessage = result.Mensaje;
                    return;
                }

                StatusMessage = result.Mensaje;
                CloudSyncSummary = $"Última sincronización: {result.FechaUtc.ToLocalTime():dd/MM/yyyy HH:mm}. " +
                                   $"{result.TotalSubidos} subida(s), {result.TotalDescargados} descarga(s), {result.TotalConflictos} conflicto(s).";

                if (result.TotalConflictos > 0)
                {
                    MessageBox.Show(
                        "Se detectaron copias con el mismo nombre y distinto contenido. No se descartó ninguna: la copia de la nube se conservó bajo un nombre alternativo.\n\n"
                        + string.Join("\n", result.Conflictos),
                        "Conflictos de sincronización",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }

                await LoadAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "SynchronizeCloudAsync failed");
                StatusMessage = $"Error al sincronizar con la nube: {ex.Message}";
            }
        }

        private async Task ManualBackupAsync()
        {
            var record = await _backupService.CreateManualBackupAsync(AuthSession.Current?.Usuario?.Email ?? "desktop-user");
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

            var record = await _backupService.RestoreBackupAsync(RestoreFilePath, AuthSession.Current?.Usuario?.Email ?? "desktop-user");
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
