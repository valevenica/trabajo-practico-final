using System;
using System.IO;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Serilog;
using StockManufactura.Domain.Entities;
using StockManufactura.Infrastructure.Db;
using StockManufactura.Shared;

namespace StockManufactura.Desktop.Infrastructure
{
    internal sealed class DatabaseResetService
    {
        public void ResetToBlank(string usuario)
        {
            var dbPath = AppPaths.DatabaseFilePath;

            // Libera el archivo aunque otros DbContext transient de esta sesion lo hayan usado antes.
            SqliteConnection.ClearAllPools();

            foreach (var suffix in new[] { string.Empty, "-wal", "-shm", "-journal" })
            {
                var path = dbPath + suffix;
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }

            var optionsBuilder = new DbContextOptionsBuilder<StockManufacturaDbContext>();
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
            using var context = new StockManufacturaDbContext(optionsBuilder.Options);
            context.Database.Migrate();
            DatabaseSeeder.EnsureSeed(context);

            // El log de auditoria tambien se borro con la base; se deja constancia del reseteo
            // en el nuevo AuditLog (unico registro que sobrevive) y en el log de archivo de Serilog.
            context.AuditLogs.Add(new AuditLog
            {
                FechaHora = DateTime.UtcNow,
                Usuario = usuario,
                Modulo = "Administracion",
                Accion = "ResetearBaseDatos",
                Entidad = "Sistema",
                IdEntidad = "-",
                Descripcion = "Se restablecio la base de datos a su estado inicial (borrado completo de datos de negocio; se recrearon los roles y el usuario administrador base).",
                Equipo = Environment.MachineName
            });
            context.SaveChanges();

            Log.Warning("Base de datos restablecida a estado inicial por {Usuario}", usuario);
        }
    }
}
