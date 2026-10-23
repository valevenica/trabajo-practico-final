#nullable enable

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using StockManufactura.Infrastructure.Backups;
using Xunit;

namespace StockManufactura.UnitTests;

/// <summary>
/// Copia y restauración de la base mientras la aplicación la tiene abierta en modo WAL, que es como funciona en producción.
/// </summary>
public sealed class DatabaseSnapshotTests : IDisposable
{
    private readonly string _carpeta = Path.Combine(Path.GetTempPath(), "snapshot-tests-" + Guid.NewGuid().ToString("N"));

    public DatabaseSnapshotTests() => Directory.CreateDirectory(_carpeta);

    [Fact]
    public async Task ExportarEImportar_ConLaBaseAbiertaEnModoWal_ConservaLoConfirmado()
    {
        var basePath = Path.Combine(_carpeta, "base.db");
        var copiaPath = Path.Combine(_carpeta, "copia.db");
        var servicio = new SqliteDatabaseSnapshotService(basePath);

        // Conexión de larga vida, como la de la aplicación; las filas confirmadas quedan en el WAL.
        using var app = new SqliteConnection($"Data Source={basePath};Pooling=False");
        app.Open();
        Ejecutar(app, "PRAGMA journal_mode=WAL; PRAGMA wal_autocheckpoint=0; CREATE TABLE Pedidos (Codigo TEXT);");
        Ejecutar(app, "INSERT INTO Pedidos VALUES ('P-1'), ('P-2');");

        await servicio.ExportAsync(copiaPath);
        Ejecutar(app, "INSERT INTO Pedidos VALUES ('P-3');");

        Assert.Equal(2L, Contar(copiaPath));

        await servicio.ImportAsync(copiaPath);

        Assert.Equal(2L, ContarEn(app));
    }

    private static void Ejecutar(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private static long ContarEn(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Pedidos";
        return (long)command.ExecuteScalar()!;
    }

    private static long Contar(string path)
    {
        using var connection = new SqliteConnection($"Data Source={path};Mode=ReadOnly;Pooling=False");
        connection.Open();
        return ContarEn(connection);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try
        {
            Directory.Delete(_carpeta, recursive: true);
        }
        catch (IOException)
        {
            // Carpeta temporal: si Windows todavía retiene un archivo, se limpia sola.
        }
    }
}
