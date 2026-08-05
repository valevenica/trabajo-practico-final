using System.Threading.Tasks;
using StockManufactura.Application.Interfaces;
using StockManufactura.Infrastructure.Db;

namespace StockManufactura.Infrastructure.Repositories
{
    public sealed class UnitOfWork : IUnitOfWork
    {
        private readonly StockManufacturaDbContext _context;

        public UnitOfWork(
            StockManufacturaDbContext context,
            IRolRepository rolRepository,
            IUsuarioRepository usuarioRepository,
            IProveedorRepository proveedorRepository,
            IRecursoRepository recursoRepository,
            IExchangeRateRepository exchangeRateRepository,
            IResourcePriceHistoryRepository resourcePriceHistoryRepository,
            IProductoRepository productoRepository,
            IOrdenProduccionRepository ordenProduccionRepository,
            IRecetaProductoItemRepository recetaProductoItemRepository,
            IStockRepository stockRepository,
            IProductCostHistoryRepository productCostHistoryRepository,
            IProductCostSnapshotRepository productCostSnapshotRepository,
            IProductCostSnapshotItemRepository productCostSnapshotItemRepository,
            IAuditLogRepository auditLogRepository,
            IBackupRecordRepository backupRecordRepository,
            IBackupSettingsRepository backupSettingsRepository,
            IRecursoProveedorRepository recursoProveedorRepository)
        {
            _context = context;
            Roles = rolRepository;
            Usuarios = usuarioRepository;
            Proveedores = proveedorRepository;
            Recursos = recursoRepository;
            ExchangeRates = exchangeRateRepository;
            ResourcePriceHistory = resourcePriceHistoryRepository;
            Productos = productoRepository;
            OrdenesProduccion = ordenProduccionRepository;
            RecetaProductoItems = recetaProductoItemRepository;
            Stocks = stockRepository;
            ProductCostHistory = productCostHistoryRepository;
            ProductCostSnapshots = productCostSnapshotRepository;
            ProductCostSnapshotItems = productCostSnapshotItemRepository;
            AuditLogs = auditLogRepository;
            BackupRecords = backupRecordRepository;
            BackupSettings = backupSettingsRepository;
            RecursoProveedores = recursoProveedorRepository;
        }

        public IRolRepository Roles { get; }
        public IUsuarioRepository Usuarios { get; }
        public IProveedorRepository Proveedores { get; }
        public IRecursoRepository Recursos { get; }
        public IExchangeRateRepository ExchangeRates { get; }
        public IResourcePriceHistoryRepository ResourcePriceHistory { get; }
        public IProductoRepository Productos { get; }
        public IOrdenProduccionRepository OrdenesProduccion { get; }
        public IRecetaProductoItemRepository RecetaProductoItems { get; }
        public IStockRepository Stocks { get; }
        public IProductCostHistoryRepository ProductCostHistory { get; }
        public IProductCostSnapshotRepository ProductCostSnapshots { get; }
        public IProductCostSnapshotItemRepository ProductCostSnapshotItems { get; }
        public IAuditLogRepository AuditLogs { get; }
        public IBackupRecordRepository BackupRecords { get; }
        public IBackupSettingsRepository BackupSettings { get; }
        public IRecursoProveedorRepository RecursoProveedores { get; }

        public Task<int> SaveChangesAsync() => _context.SaveChangesAsync();
    }
}
