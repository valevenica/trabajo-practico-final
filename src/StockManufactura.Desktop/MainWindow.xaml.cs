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
        TryLoadWindowIcon();
    }

    private void TryLoadWindowIcon()
    {
        try
        {
            var icon = DesktopAssetLoader.TryLoadWindowIcon();
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