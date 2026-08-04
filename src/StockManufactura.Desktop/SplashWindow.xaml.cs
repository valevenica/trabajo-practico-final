using System;
using System.Windows;
using StockManufactura.Desktop.Infrastructure;

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
            var logo = DesktopAssetLoader.TryLoadLogoImage();
            if (logo is not null)
            {
                LogoImage.Source = logo;
                FallbackText.Visibility = Visibility.Collapsed;
                return;
            }

            FallbackText.Visibility = Visibility.Visible;
        }
    }
}
