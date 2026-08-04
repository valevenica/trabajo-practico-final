using System;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using StockManufactura.Shared;
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
            if (TryLoadFromUri("pack://application:,,,/Assets/logo%202.png") ||
                TryLoadFromUri("pack://application:,,,/Assets/logo.png"))
            {
                return;
            }

            var candidatePaths = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "Assets", "logo 2.png"),
                Path.Combine(AppContext.BaseDirectory, "Assets", "logo.png"),
                Path.Combine(AppPaths.AssetsDirectory, "logo 2.png"),
                AppPaths.SplashLogoPath
            };

            var candidate = candidatePaths.FirstOrDefault(File.Exists);
            if (string.IsNullOrWhiteSpace(candidate))
            {
                return;
            }

            TryLoadFromUri(candidate);
        }

        private bool TryLoadFromUri(string uri)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(uri, UriKind.Absolute);
                bitmap.EndInit();
                LoginLogoImage.Source = bitmap;
                return true;
            }
            catch
            {
                return false;
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
    }
}
