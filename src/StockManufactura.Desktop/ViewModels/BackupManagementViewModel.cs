using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StockManufactura.Application.Interfaces;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Desktop.ViewModels
{
    public sealed partial class BackupManagementViewModel : ObservableObject
    {
        private readonly IBackupService _backupService;

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

        public BackupManagementViewModel(IBackupService backupService)
        {
            _backupService = backupService;
            Records = new ObservableCollection<BackupRecord>();
            SaveSettingsCommand = new AsyncRelayCommand(SaveSettingsAsync);
            ManualBackupCommand = new AsyncRelayCommand(ManualBackupAsync);
            RestoreBackupCommand = new AsyncRelayCommand(RestoreBackupAsync);
            RefreshCommand = new AsyncRelayCommand(LoadAsync);
            _ = LoadAsync();
        }

        public ObservableCollection<BackupRecord> Records { get; }

        public ICommand SaveSettingsCommand { get; }
        public ICommand ManualBackupCommand { get; }
        public ICommand RestoreBackupCommand { get; }
        public ICommand RefreshCommand { get; }

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
    }
}
