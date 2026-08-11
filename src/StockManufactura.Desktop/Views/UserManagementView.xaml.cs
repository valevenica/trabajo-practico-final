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

        private void OnNewPasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is UserManagementViewModel viewModel && sender is PasswordBox passwordBox)
            {
                viewModel.NewPassword = passwordBox.Password;
            }
        }

        private void OnMyCurrentPasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is UserManagementViewModel viewModel && sender is PasswordBox passwordBox)
            {
                viewModel.MyCurrentPassword = passwordBox.Password;
            }
        }

        private void OnMyNewPasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is UserManagementViewModel viewModel && sender is PasswordBox passwordBox)
            {
                viewModel.MyNewPassword = passwordBox.Password;
            }
        }
    }
}
