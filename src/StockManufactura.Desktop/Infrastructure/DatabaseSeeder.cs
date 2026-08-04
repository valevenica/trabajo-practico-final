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
        public const string AdminEmail = "admin@stockmanufactura.local";
        private const string AdminName = "Administrador";
        private const string AdminPassword = "Admin123!";

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
                dbContext.Roles.Add(adminRole);
                hasChanges = true;
            }

            var operatorRole = dbContext.Roles.FirstOrDefault(r => r.Nombre == OperatorRoleName);
            if (operatorRole is null)
            {
                operatorRole = new Rol(OperatorRoleName, "Rol operativo con permisos limitados.");
                dbContext.Roles.Add(operatorRole);
                hasChanges = true;
            }

            var adminUser = dbContext.Usuarios.FirstOrDefault(u => u.Email == AdminEmail);
            if (adminUser is null)
            {
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(AdminPassword);
                adminUser = new Usuario(AdminName, AdminEmail, passwordHash, adminRole.Id);
                adminUser.AsignarRol(adminRole);
                dbContext.Usuarios.Add(adminUser);
                hasChanges = true;
            }

            if (hasChanges)
            {
                dbContext.SaveChanges();
            }

            var after = GetSeedStatus(dbContext);
            var wasRecreated = !before.IsComplete && after.IsComplete;
            return new SeedResult(before, after, wasRecreated);
        }
    }
}
