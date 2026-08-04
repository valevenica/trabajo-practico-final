using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using StockManufactura.Desktop.Extensions;
using StockManufactura.Desktop.Infrastructure;
using StockManufactura.Desktop.Services;
using StockManufactura.Desktop.ViewModels;
using StockManufactura.Infrastructure.Db;

namespace StockManufactura.Desktop;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : global::System.Windows.Application
{
    private IHost? _host;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File("logs\\StockManufactura.log", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        _host = Host.CreateDefaultBuilder()
            .UseSerilog()
            .ConfigureServices((context, services) =>
            {
                services.AddDesktopServices();
                services.AddDesktopInfrastructure();
            })
            .Build();

        using (var scope = _host.Services.CreateScope())
        {
            var scopedServices = scope.ServiceProvider;
            var healthCheck = scopedServices.GetRequiredService<DesktopStartupHealthCheck>();
            var healthReport = healthCheck.Execute();
            WriteStartupDiagnostics(healthReport);

            if (e.Args.Any(arg => string.Equals(arg, "--health-check", StringComparison.OrdinalIgnoreCase)))
            {
                Shutdown(0);
                return;
            }
        }

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        var mainWindowViewModel = _host.Services.GetRequiredService<MainWindowViewModel>();
        var loginViewModel = _host.Services.GetRequiredService<LoginViewModel>();
        var navigationService = _host.Services.GetRequiredService<NavigationService>();

        navigationService.NavigateAction = viewModel => mainWindowViewModel.CurrentViewModel = viewModel;
        mainWindow.DataContext = mainWindowViewModel;
        mainWindowViewModel.CurrentViewModel = loginViewModel;
        mainWindow.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        Log.CloseAndFlush();

        base.OnExit(e);
    }

    private static void WriteStartupDiagnostics(DesktopStartupHealthCheckReport report)
    {
        var diagnosticText = report.ToDiagnosticText();
        Console.WriteLine(diagnosticText);
        Debug.WriteLine(diagnosticText);
        Trace.WriteLine(diagnosticText);
        Log.Information("{DesktopHealthCheckReport}", diagnosticText);

        Directory.CreateDirectory("logs");
        File.AppendAllText("logs\\startup-healthcheck.log", $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {Environment.NewLine}{diagnosticText}{Environment.NewLine}");
    }
}

