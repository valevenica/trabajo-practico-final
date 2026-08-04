using System.Windows.Controls;
using StockManufactura.Desktop.ViewModels;

namespace StockManufactura.Desktop.Views
{
    public partial class UserManagementView : UserControl
    {
        public UserManagementView()
        {
            InitializeComponent();
        }

        private void OnCreatePasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is UserManagementViewModel viewModel && sender is PasswordBox passwordBox)
            {
                viewModel.Password = passwordBox.Password;
            }
        }

        private void OnResetPasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is UserManagementViewModel viewModel && sender is PasswordBox passwordBox)
            {
                viewModel.ResetPassword = passwordBox.Password;
            }
        }
    }
}
