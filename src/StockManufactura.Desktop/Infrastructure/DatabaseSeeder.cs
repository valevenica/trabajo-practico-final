using System;
using System.Linq;
using StockManufactura.Domain.Entities;
using StockManufactura.Infrastructure.Db;

namespace StockManufactura.Desktop.Infrastructure
{
    internal static class DatabaseSeeder
    {
        public const string AdminRoleName = "Administrador";
        public const string OperatorRoleName = "Operador";
        public const string AdminEmail = "admin@test.com";
        private const string LegacyAdminEmail = "admin@stockmanufactura.local";
        private const string AdminName = "Admin";
        private const string DefaultAdminPassword = "Admin123";

        internal readonly record struct SeedStatus(bool AdminRoleExists, bool OperatorRoleExists, bool AdminUserExists)
        {
            public bool IsComplete => AdminRoleExists && OperatorRoleExists && AdminUserExists;
        }

        internal readonly record struct SeedResult(SeedStatus Before, SeedStatus After, bool WasRecreated);

        public static SeedStatus GetSeedStatus(StockManufacturaDbContext dbContext)
        {
            var adminRoleExists = dbContext.Roles.Any(r => r.Nombre == AdminRoleName);
            var operatorRoleExists = dbContext.Roles.Any(r => r.Nombre == OperatorRoleName);
            var adminUserExists = dbContext.Usuarios.Any(u => u.Email == AdminEmail);

            return new SeedStatus(adminRoleExists, operatorRoleExists, adminUserExists);
        }

        public static void Seed(StockManufacturaDbContext dbContext)
        {
            EnsureSeed(dbContext);
        }

        public static SeedResult EnsureSeed(StockManufacturaDbContext dbContext)
        {
            var before = GetSeedStatus(dbContext);
            var hasChanges = false;

            var adminRole = dbContext.Roles.FirstOrDefault(r => r.Nombre == AdminRoleName);
            if (adminRole is null)
            {
                adminRole = new Rol(AdminRoleName, "Rol con acceso completo al sistema.");
                adminRole.AsignarPermisos(new[]
                {
                    "PRODUCTOS_VER", "PRODUCTOS_CREAR", "PRODUCTOS_EDITAR",
                    "STOCK_VER", "STOCK_AJUSTAR",
                    "COSTOS_VER",
                    "USUARIOS_ADMIN"
                });
                dbContext.Roles.Add(adminRole);
                hasChanges = true;
            }

            var operatorRole = dbContext.Roles.FirstOrDefault(r => r.Nombre == OperatorRoleName);
            if (operatorRole is null)
            {
                operatorRole = new Rol(OperatorRoleName, "Rol operativo con permisos limitados.");
                operatorRole.AsignarPermisos(new[]
                {
                    "PRODUCTOS_VER",
                    "STOCK_VER"
                });
                dbContext.Roles.Add(operatorRole);
                hasChanges = true;
            }

            var adminUser = dbContext.Usuarios.FirstOrDefault(u => u.Email == AdminEmail)
                ?? dbContext.Usuarios.FirstOrDefault(u => u.Email == LegacyAdminEmail);
            if (adminUser is null)
            {
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(GetAdminPassword());
                adminUser = new Usuario(AdminName, AdminEmail, passwordHash, adminRole.Id);
                adminUser.AsignarRol(adminRole);
                dbContext.Usuarios.Add(adminUser);
                hasChanges = true;
            }
            else
            {
                var configuredPassword = GetAdminPassword();
                var adminUserChanged = false;
                var shouldUpdateIdentity = !string.Equals(adminUser.Email, AdminEmail, StringComparison.OrdinalIgnoreCase)
                    || !string.Equals(adminUser.Nombre, AdminName, StringComparison.Ordinal);
                var shouldUpdatePassword = !BCrypt.Net.BCrypt.Verify(configuredPassword, adminUser.PasswordHash);
                var shouldDisableForcedChange = adminUser.RequiereCambioPassword;

                if (shouldUpdateIdentity)
                {
                    adminUser.ActualizarDatos(AdminName, AdminEmail);
                    adminUserChanged = true;
                    hasChanges = true;
                }

                if (shouldUpdatePassword)
                {
                    adminUser.CambiarPasswordHash(BCrypt.Net.BCrypt.HashPassword(configuredPassword));
                    adminUserChanged = true;
                    hasChanges = true;
                }
                else if (shouldDisableForcedChange)
                {
                    // Clear forced-password flag without changing hash.
                    adminUser.CambiarPasswordHash(adminUser.PasswordHash);
                    adminUserChanged = true;
                    hasChanges = true;
                }

                if (adminUserChanged)
                {
                    dbContext.Usuarios.Update(adminUser);
                }
            }

            if (hasChanges)
            {
                dbContext.SaveChanges();
            }

            var after = GetSeedStatus(dbContext);
            var wasRecreated = !before.IsComplete && after.IsComplete;
            return new SeedResult(before, after, wasRecreated);
        }

        private static string GetAdminPassword()
        {
            var configured = Environment.GetEnvironmentVariable("STOCKMANUFACTURA_ADMIN_PASSWORD");
            return string.IsNullOrWhiteSpace(configured) ? DefaultAdminPassword : configured;
        }
    }
}
