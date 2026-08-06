using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StockManufactura.Application.Interfaces;
using StockManufactura.Application.Mappings;
using StockManufactura.Application.Services;
using StockManufactura.Desktop.Infrastructure;
using StockManufactura.Desktop.Services;
using StockManufactura.Infrastructure.Db;
using StockManufactura.Infrastructure.Monetary.Providers;
using StockManufactura.Infrastructure.Repositories;
using StockManufactura.Desktop.ViewModels;
using StockManufactura.Shared;

namespace StockManufactura.Desktop.Extensions
{
    internal static class DesktopServiceCollectionExtensions
    {
        public static IServiceCollection AddDesktopServices(this IServiceCollection services)
        {
            services.AddSingleton<NavigationService>();
            services.AddSingleton<MainWindowViewModel>();
            services.AddTransient<LoginViewModel>();
            services.AddTransient<ResourceManagementViewModel>();
            services.AddTransient<MonetaryConfigurationViewModel>();
            services.AddTransient<AuditLogViewModel>();
            services.AddTransient<BackupManagementViewModel>();
            services.AddTransient<ProductCostHistoryViewModel>();
            services.AddSingleton<MainWindow>();
            return services;
        }

        public static IServiceCollection AddDesktopInfrastructure(this IServiceCollection services)
        {
            var dataDirectory = AppPaths.DataDirectory;
            Directory.CreateDirectory(dataDirectory);
            var connectionString = $"Data Source={AppPaths.DatabaseFilePath}";

            services.AddDbContext<StockManufacturaDbContext>(options => options.UseSqlite(connectionString), ServiceLifetime.Transient);
            services.AddAutoMapper(cfg => cfg.AddProfile<StockManufacturaMappingProfile>());
            // Individual repos needed by Application services that inject them directly
            services.AddTransient(typeof(IRepository<>), typeof(Repository<>));
            services.AddTransient<IRolRepository, RolRepository>();
            services.AddTransient<IUsuarioRepository, UsuarioRepository>();
            services.AddTransient<IProveedorRepository, ProveedorRepository>();
            services.AddTransient<IRecursoRepository, RecursoRepository>();
            services.AddTransient<IExchangeRateRepository, ExchangeRateRepository>();
            services.AddTransient<IResourcePriceHistoryRepository, ResourcePriceHistoryRepository>();
            services.AddTransient<IProductoRepository, ProductoRepository>();
            services.AddTransient<IOrdenProduccionRepository, OrdenProduccionRepository>();
            services.AddTransient<IRecetaProductoItemRepository, RecetaProductoItemRepository>();
            services.AddTransient<IStockRepository, StockRepository>();
            services.AddTransient<IProductCostHistoryRepository, ProductCostHistoryRepository>();
            services.AddTransient<IProductCostSnapshotRepository, ProductCostSnapshotRepository>();
            services.AddTransient<IProductCostSnapshotItemRepository, ProductCostSnapshotItemRepository>();
            services.AddTransient<IAuditLogRepository, AuditLogRepository>();
            services.AddTransient<IBackupRecordRepository, BackupRecordRepository>();
            services.AddTransient<IBackupSettingsRepository, BackupSettingsRepository>();
            services.AddTransient<IRecursoProveedorRepository, RecursoProveedorRepository>();
            // UnitOfWork creates its own repos from its own DbContext — ensures SaveChangesAsync persists correctly
            services.AddTransient<IUnitOfWork, UnitOfWork>();

            services.AddSingleton<IExchangeRateProvider, DolarHoyProvider>();
            services.AddSingleton<IExchangeRateProvider, BluelyticsExchangeRateProvider>();
            services.AddTransient<IGoogleDriveBackupSyncService, NoOpGoogleDriveBackupSyncService>();

            services.AddTransient<IAuditLogService, AuditLogService>();
            services.AddTransient<IBackupService, BackupService>();
            services.AddTransient<IAuthenticationService, AuthenticationService>();
            services.AddTransient<IProductCostService, ProductCostService>();
            services.AddTransient<ICostReportService, CostReportService>();
            services.AddTransient<IMonetaryConfigurationService, MonetaryConfigurationService>();
            services.AddTransient<ISystemStatusService, SystemStatusService>();
            services.AddTransient<IResourcePricingService, ResourcePricingService>();
            services.AddTransient<IUserManagementService, UserManagementService>();
            services.AddTransient<DesktopStartupHealthCheck>();
            return services;
        }
    }
}
