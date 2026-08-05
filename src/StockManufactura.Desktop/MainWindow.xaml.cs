using System;
using System.ComponentModel;
using System.Windows;
using StockManufactura.Desktop.Infrastructure;
using StockManufactura.Desktop.ViewModels;

namespace StockManufactura.Desktop;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private const double LoginWidth = 860;
    private const double LoginHeight = 620;
    private const double LoginMinWidth = 760;
    private const double LoginMinHeight = 520;
    private const double ShellWidth = 1120;
    private const double ShellHeight = 760;
    private const double ShellMinWidth = 900;
    private const double ShellMinHeight = 560;

    private MainWindowViewModel? _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        TryLoadHeaderLogo();
        TryLoadWindowIcon();
        DataContextChanged += OnDataContextChanged;
        Loaded += (_, _) => ApplyWindowLayoutForCurrentState();
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        _viewModel = e.NewValue as MainWindowViewModel;
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        ApplyWindowLayoutForCurrentState();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.IsShellVisible))
        {
            ApplyWindowLayoutForCurrentState();
        }
    }

    private void ApplyWindowLayoutForCurrentState()
    {
        var shellVisible = _viewModel?.IsShellVisible == true;

        if (shellVisible)
        {
            MinWidth = ShellMinWidth;
            MinHeight = ShellMinHeight;
            Width = ShellWidth;
            Height = ShellHeight;
            WindowState = WindowState.Maximized;
        }
        else
        {
            MinWidth = LoginMinWidth;
            MinHeight = LoginMinHeight;
            Width = LoginWidth;
            Height = LoginHeight;
            WindowState = WindowState.Normal;
        }

        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        UpdateLayout();
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