using Microsoft.EntityFrameworkCore;
using StockManufactura.Application.Email;
using StockManufactura.Application.Interfaces;
using StockManufactura.Application.Mappings;
using StockManufactura.Application.Services;
using StockManufactura.Infrastructure.Backups;
using StockManufactura.Infrastructure.Cloud;
using StockManufactura.Infrastructure.Db;
using StockManufactura.Infrastructure.Email;
using StockManufactura.Infrastructure.Import;
using StockManufactura.Infrastructure.Monetary.Providers;
using StockManufactura.Infrastructure.Repositories;
using StockManufactura.Shared;

namespace StockManufactura.Web.Infrastructure
{
    internal static class WebServiceCollectionExtensions
    {
        /// <summary>
        /// Mismo registro que la versión de escritorio. El DbContext sigue siendo transitorio: cada página resuelve sus
        /// servicios desde su propio ámbito (<see cref="Components.PaginaBase"/>), equivalente al contexto por pantalla del WPF.
        /// </summary>
        public static IServiceCollection AddStockManufacturaWeb(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("StockManufactura");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                // Por defecto se usa la misma base que la versión de escritorio, para trabajar sobre los mismos datos.
                Directory.CreateDirectory(AppPaths.DataDirectory);
                connectionString = $"Data Source={AppPaths.DatabaseFilePath}";
            }

            services.AddDbContext<StockManufacturaDbContext>(options => options.UseSqlite(connectionString), ServiceLifetime.Transient);
            services.AddAutoMapper(cfg => cfg.AddProfile<StockManufacturaMappingProfile>());
            services.AddTransient(typeof(IRepository<>), typeof(Repository<>));
            services.AddTransient<IRolRepository, RolRepository>();
            services.AddTransient<IUsuarioRepository, UsuarioRepository>();
            services.AddTransient<IProveedorRepository, ProveedorRepository>();
            services.AddTransient<IClienteRepository, ClienteRepository>();
            services.AddTransient<IRecursoRepository, RecursoRepository>();
            services.AddTransient<IExchangeRateRepository, ExchangeRateRepository>();
            services.AddTransient<IResourcePriceHistoryRepository, ResourcePriceHistoryRepository>();
            services.AddTransient<IProductoRepository, ProductoRepository>();
            services.AddTransient<IOrdenProduccionRepository, OrdenProduccionRepository>();
            services.AddTransient<IRecetaProductoItemRepository, RecetaProductoItemRepository>();
            services.AddTransient<IProductCostHistoryRepository, ProductCostHistoryRepository>();
            services.AddTransient<IProductCostSnapshotRepository, ProductCostSnapshotRepository>();
            services.AddTransient<IProductCostSnapshotItemRepository, ProductCostSnapshotItemRepository>();
            services.AddTransient<IAuditLogRepository, AuditLogRepository>();
            services.AddTransient<IBackupRecordRepository, BackupRecordRepository>();
            services.AddTransient<IBackupSettingsRepository, BackupSettingsRepository>();
            services.AddTransient<IRecursoProveedorRepository, RecursoProveedorRepository>();
            services.AddTransient<IRecursoTramoPrecioRepository, RecursoTramoPrecioRepository>();
            services.AddTransient<IUnitOfWork, UnitOfWork>();

            services.AddSingleton<IExchangeRateProvider, DolarHoyProvider>();
            services.AddSingleton<IExchangeRateProvider, BluelyticsExchangeRateProvider>();
            services.AddTransient<ICloudBackupSyncService, FolderCloudBackupSyncService>();

            services.AddTransient<IAuditLogService, AuditLogService>();
            services.AddTransient<IBackupService, BackupService>();
            services.AddSingleton<IDatabaseSnapshotService, SqliteDatabaseSnapshotService>();
            services.AddTransient<IAuthenticationService, AuthenticationService>();
            services.AddTransient<IProductCostService, ProductCostService>();
            services.AddTransient<ICostReportService, CostReportService>();
            services.AddTransient<IBusinessIntelligenceService, BusinessIntelligenceService>();
            services.AddTransient<IMonetaryConfigurationService, MonetaryConfigurationService>();
            services.AddTransient<ISystemStatusService, SystemStatusService>();
            services.AddTransient<IShortageService, ShortageService>();
            services.AddTransient<IResourcePricingService, ResourcePricingService>();
            services.AddTransient<ICatalogActivationService, CatalogActivationService>();
            services.AddTransient<IStockIntakeService, StockIntakeService>();
            services.AddTransient<IResourcePriceImportService, ClosedXmlResourcePriceImportService>();
            services.AddTransient<IUserManagementService, UserManagementService>();
            services.AddSingleton<PasswordRecoveryCodeStore>();
            services.AddSingleton<IEmailSettingsStore, JsonEmailSettingsStore>();
            services.AddTransient<IEmailSender, SmtpEmailSender>();
            services.AddTransient<IPasswordRecoveryService, PasswordRecoveryService>();

            services.AddScoped<WebSession>();
            services.AddHostedService<AutomaticBackupService>();
            return services;
        }

        /// <summary>Aplica las migraciones pendientes y garantiza los roles y el usuario administrador iniciales.</summary>
        public static void InitializeDatabase(this IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<StockManufacturaDbContext>();
            context.Database.Migrate();
            DatabaseSeeder.EnsureSeed(context);
        }
    }
}
