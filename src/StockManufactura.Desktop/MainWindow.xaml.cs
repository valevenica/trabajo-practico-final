using System;
using System.Windows;
using StockManufactura.Desktop.Infrastructure;

namespace StockManufactura.Desktop;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        TryLoadHeaderLogo();
        TryLoadWindowIcon();
    }

    private void TryLoadHeaderLogo()
    {
        try
        {
            var logo = DesktopAssetLoader.TryLoadLogoImage();
            if (logo is not null)
            {
                HeaderLogoImage.Source = logo;
            }
        }
        catch
        {
            // Keep header without logo if asset loading fails.
        }
    }

    private void TryLoadWindowIcon()
    {
        try
        {
            var icon = DesktopAssetLoader.TryLoadWindowIcon();
            if (icon is null)
            {
                icon = DesktopAssetLoader.TryLoadLogoImage();
            }

            if (icon is not null)
            {
                Icon = icon;
            }
        }
        catch
        {
            // Keep default icon if custom asset cannot be loaded.
        }
    }
}