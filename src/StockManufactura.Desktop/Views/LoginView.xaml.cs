using System.Windows.Controls;
using System.Windows.Input;
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
