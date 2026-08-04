using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using StockManufactura.Shared;

namespace StockManufactura.Desktop
{
    public partial class SplashWindow : Window
    {
        public SplashWindow()
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
                FallbackText.Visibility = Visibility.Visible;
                return;
            }

            try
            {
                if (!TryLoadFromUri(candidate))
                {
                    FallbackText.Visibility = Visibility.Visible;
                }
            }
            catch
            {
                FallbackText.Visibility = Visibility.Visible;
            }
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
                LogoImage.Source = bitmap;
                FallbackText.Visibility = Visibility.Collapsed;
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
