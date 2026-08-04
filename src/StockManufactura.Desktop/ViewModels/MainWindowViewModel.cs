using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace StockManufactura.Desktop.ViewModels
{
    public sealed class MainWindowViewModel : INotifyPropertyChanged
    {
        private object _currentViewModel = default!;

        public MainWindowViewModel()
        {
        }

        public object CurrentViewModel
        {
            get => _currentViewModel;
            set
            {
                if (Equals(_currentViewModel, value))
                {
                    return;
                }
                _currentViewModel = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
