using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StockManufactura.Application.Interfaces;
using StockManufactura.Desktop.Infrastructure;
using StockManufactura.Domain.Entities;
using StockManufactura.Infrastructure.Db;
using StockManufactura.Infrastructure.Repositories;
using StockManufactura.Desktop.Services;
using StockManufactura.Desktop.ViewModels;

namespace StockManufactura.Desktop.Extensions
{
    internal static class DesktopServiceCollectionExtensions
    {
        public static IServiceCollection AddDesktopServices(this IServiceCollection services)
        {
            services.AddSingleton<NavigationService>();
            services.AddSingleton<MainWindowViewModel>();
            services.AddTransient<LoginViewModel>();
            services.AddSingleton<MainWindow>();
            return services;
        }

        public static IServiceCollection AddDesktopInfrastructure(this IServiceCollection services)
        {
            var dataDirectory = Path.Combine(AppContext.BaseDirectory, "Data");
            Directory.CreateDirectory(dataDirectory);
            var connectionString = $"Data Source={Path.Combine(dataDirectory, "StockManufactura.db")}";

            services.AddDbContext<StockManufacturaDbContext>(options => options.UseSqlite(connectionString), ServiceLifetime.Transient);
            services.AddTransient(typeof(IRepository<>), typeof(Repository<>));
            services.AddTransient<IRolRepository, RolRepository>();
            services.AddTransient<IUsuarioRepository, UsuarioRepository>();
            services.AddTransient<IUnitOfWork, UnitOfWork>();
            services.AddTransient<DesktopStartupHealthCheck>();
            return services;
        }
    }
}
