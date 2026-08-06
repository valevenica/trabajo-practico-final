using System.Windows.Controls;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using StockManufactura.Desktop.Infrastructure;
using StockManufactura.Desktop.ViewModels;

namespace StockManufactura.Desktop.Views
{
    public partial class LoginView : UserControl
    {
        public LoginView()
        {
            InitializeComponent();
            TryLoadLogo();
            DataContextChanged += (s, e) =>
            {
                if (DataContext is ObservableObject oldVm)
                    oldVm.PropertyChanged -= OnViewModelPropertyChanged;
                if (DataContext is ObservableObject newVm)
                    newVm.PropertyChanged += OnViewModelPropertyChanged;
            };
        }

        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LoginViewModel.ShowPassword))
            {
                OnShowPasswordToggled();
            }
        }

        private void TryLoadLogo()
        {
            var logo = DesktopAssetLoader.TryLoadLogoImage();
            if (logo is not null)
            {
                LoginLogoImage.Source = logo;
            }
        }

        private void OnPasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel viewModel && sender is PasswordBox passwordBox)
            {
                viewModel.Password = passwordBox.Password;
            }
        }

        public void OnShowPasswordToggled()
        {
            // Transferir foco al control visible después del toggle
            if (DataContext is LoginViewModel viewModel)
            {
                if (viewModel.ShowPassword)
                {
                    VisiblePasswordBox?.Focus();
                }
                else
                {
                    PasswordBox?.Focus();
                }
            }
        }

        private void OnNewPasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel viewModel && sender is PasswordBox passwordBox)
            {
                viewModel.NewPassword = passwordBox.Password;
            }
        }

        private void OnConfirmNewPasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel viewModel && sender is PasswordBox passwordBox)
            {
                viewModel.ConfirmNewPassword = passwordBox.Password;
            }
        }

        private void OnLoginEnterPressed(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
            {
                return;
            }

            if (DataContext is not LoginViewModel viewModel)
            {
                return;
            }

            if (viewModel.LoginCommand.CanExecute(null))
            {
                viewModel.LoginCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}
