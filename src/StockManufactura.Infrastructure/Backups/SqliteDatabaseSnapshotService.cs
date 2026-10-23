using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using StockManufactura.Application.Interfaces;
using StockManufactura.Shared;

namespace StockManufactura.Infrastructure.Backups
{
    /// <summary>
    /// Copia en línea de SQLite (<c>sqlite3_backup</c>): funciona con la base abierta y en modo WAL, e incluye las
    /// transacciones confirmadas que todavía no se trasladaron del WAL al archivo principal. Copiar el archivo .db
    /// directamente falla mientras la aplicación lo tiene abierto y, aun cuando no falla, puede omitir esas transacciones.
    /// </summary>
    public sealed class SqliteDatabaseSnapshotService : IDatabaseSnapshotService
    {
        private readonly string _databasePath;

        public SqliteDatabaseSnapshotService()
            : this(AppPaths.DatabaseFilePath)
        {
        }

        public SqliteDatabaseSnapshotService(string databasePath)
        {
            _databasePath = databasePath;
        }

        public Task ExportAsync(string destinationPath, CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
            {
                using var source = new SqliteConnection($"Data Source={_databasePath}");
                // Sin pooling: el archivo de destino debe quedar liberado para comprimirlo y borrarlo.
                using var destination = new SqliteConnection($"Data Source={destinationPath};Pooling=False");
                source.Open();
                destination.Open();
                source.BackupDatabase(destination);
            }, cancellationToken);
        }

        public Task ImportAsync(string sourcePath, CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
            {
                using var source = new SqliteConnection($"Data Source={sourcePath};Mode=ReadOnly;Pooling=False");
                using var destination = new SqliteConnection($"Data Source={_databasePath}");
                source.Open();
                destination.Open();
                source.BackupDatabase(destination);
            }, cancellationToken);
        }
    }
}
