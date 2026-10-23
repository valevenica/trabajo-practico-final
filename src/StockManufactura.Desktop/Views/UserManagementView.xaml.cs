using System.Windows.Controls;
using StockManufactura.Desktop.ViewModels;

namespace StockManufactura.Desktop.Views
{
    public partial class UserManagementView : UserControl
    {
        public UserManagementView()
        {
            InitializeComponent();
            DataContextChanged += (_, e) =>
            {
                if (e.OldValue is UserManagementViewModel oldVm)
                    oldVm.PropertyChanged -= OnViewModelPropertyChanged;
                if (e.NewValue is UserManagementViewModel newVm)
                    newVm.PropertyChanged += OnViewModelPropertyChanged;
            };
        }

        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Al ocultar el cambio de contraseña (Guardar o Cancelar) se limpia lo escrito: los PasswordBox
            // no admiten enlace de datos, por eso se vacían desde la vista.
            if (e.PropertyName == nameof(UserManagementViewModel.IsMyChangePasswordVisible)
                && sender is UserManagementViewModel { IsMyChangePasswordVisible: false })
            {
                MyCurrentPasswordBox.Clear();
                MyNewPasswordBox.Clear();
            }
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

        private void OnSmtpPasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is UserManagementViewModel viewModel && sender is PasswordBox passwordBox)
            {
                viewModel.SmtpPassword = passwordBox.Password;
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
