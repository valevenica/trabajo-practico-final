using System.Threading.Tasks;

namespace StockManufactura.Application.Interfaces
{
    public interface IUnitOfWork
    {
        IRolRepository Roles { get; }
        IUsuarioRepository Usuarios { get; }
        IProveedorRepository Proveedores { get; }
        IRecursoRepository Recursos { get; }
        IExchangeRateRepository ExchangeRates { get; }
        IResourcePriceHistoryRepository ResourcePriceHistory { get; }
        IProductoRepository Productos { get; }
        IRecetaProductoItemRepository RecetaProductoItems { get; }
        IStockRepository Stocks { get; }
        IProductCostHistoryRepository ProductCostHistory { get; }
        IProductCostSnapshotRepository ProductCostSnapshots { get; }
        IProductCostSnapshotItemRepository ProductCostSnapshotItems { get; }
        IAuditLogRepository AuditLogs { get; }
        IBackupRecordRepository BackupRecords { get; }
        IBackupSettingsRepository BackupSettings { get; }
        Task<int> SaveChangesAsync();
    }
}
