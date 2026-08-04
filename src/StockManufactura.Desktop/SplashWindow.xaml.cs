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
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(candidate, UriKind.Absolute);
                bitmap.EndInit();
                LogoImage.Source = bitmap;
            }
            catch
            {
                FallbackText.Visibility = Visibility.Visible;
            }
        }
    }
}
