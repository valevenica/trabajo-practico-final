using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.EntityFrameworkCore;
using StockManufactura.Infrastructure.Db;

namespace StockManufactura.Desktop.Infrastructure
{
    internal sealed class DesktopStartupHealthCheck
    {
        private readonly StockManufacturaDbContext _dbContext;

        public DesktopStartupHealthCheck(StockManufacturaDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public DesktopStartupHealthCheckReport Execute()
        {
            var report = new DesktopStartupHealthCheckReport();

            var dbPath = _dbContext.Database.GetDbConnection().DataSource;
            report.DatabasePath = dbPath;
            report.DatabaseExistsBefore = File.Exists(dbPath);

            var totalMigrations = _dbContext.Database.GetMigrations().ToList();
            report.TotalMigrations = totalMigrations.Count;

            if (totalMigrations.Any())
            {
                _dbContext.Database.Migrate();
                report.MigrationMode = "Migrate";
            }
            else
            {
                _dbContext.Database.EnsureCreated();
                report.MigrationMode = "EnsureCreated";
            }

            report.DatabaseExistsAfter = File.Exists(dbPath);
            report.AppliedMigrations = _dbContext.Database.GetAppliedMigrations().Count();
            report.PendingMigrations = _dbContext.Database.GetPendingMigrations().Count();

            var seedResult = DatabaseSeeder.EnsureSeed(_dbContext);
            report.SeedStatusBefore = seedResult.Before;
            report.SeedStatusAfter = seedResult.After;
            report.SeedRecreated = seedResult.WasRecreated;

            report.RolesCount = _dbContext.Roles.Count();
            report.UsersCount = _dbContext.Usuarios.Count();
            report.AdminUserExists = _dbContext.Usuarios.Any(u => u.Email == DatabaseSeeder.AdminEmail);

            return report;
        }
    }

    internal sealed class DesktopStartupHealthCheckReport
    {
        public string DatabasePath { get; set; } = string.Empty;
        public bool DatabaseExistsBefore { get; set; }
        public bool DatabaseExistsAfter { get; set; }
        public int TotalMigrations { get; set; }
        public int AppliedMigrations { get; set; }
        public int PendingMigrations { get; set; }
        public string MigrationMode { get; set; } = string.Empty;
        public int RolesCount { get; set; }
        public int UsersCount { get; set; }
        public bool AdminUserExists { get; set; }
        public DatabaseSeeder.SeedStatus SeedStatusBefore { get; set; }
        public DatabaseSeeder.SeedStatus SeedStatusAfter { get; set; }
        public bool SeedRecreated { get; set; }

        public bool SeedHealthy => SeedStatusAfter.IsComplete;

        public string ToDiagnosticText()
        {
            var builder = new StringBuilder();
            builder.AppendLine("[Desktop Health Check]");
            builder.AppendLine($"- SQLite Path: {DatabasePath}");
            builder.AppendLine($"- DB Exists (before): {DatabaseExistsBefore}");
            builder.AppendLine($"- DB Exists (after): {DatabaseExistsAfter}");
            builder.AppendLine($"- Migration Mode: {MigrationMode}");
            builder.AppendLine($"- Migrations (total/applied/pending): {TotalMigrations}/{AppliedMigrations}/{PendingMigrations}");
            builder.AppendLine($"- Roles Count: {RolesCount}");
            builder.AppendLine($"- Usuarios Count: {UsersCount}");
            builder.AppendLine($"- Admin Exists: {AdminUserExists}");
            builder.AppendLine($"- Seed Status (before): {FormatSeedStatus(SeedStatusBefore)}");
            builder.AppendLine($"- Seed Status (after): {FormatSeedStatus(SeedStatusAfter)}");
            builder.AppendLine($"- Seed Recreated: {SeedRecreated}");
            builder.AppendLine($"- Seed Healthy: {SeedHealthy}");
            return builder.ToString();
        }

        private static string FormatSeedStatus(DatabaseSeeder.SeedStatus status)
        {
            return $"AdminRole={status.AdminRoleExists}, OperatorRole={status.OperatorRoleExists}, AdminUser={status.AdminUserExists}, Complete={status.IsComplete}";
        }
    }
}
