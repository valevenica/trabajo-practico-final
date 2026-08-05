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

            // Seed realistic demo data for a soy-candle business with own oil extraction.
            if (EnsureSoyCandleDemoData(dbContext))
            {
                dbContext.SaveChanges();
            }

            var after = GetSeedStatus(dbContext);
            var wasRecreated = !before.IsComplete && after.IsComplete;
            return new SeedResult(before, after, wasRecreated);
        }

        private static bool EnsureSoyCandleDemoData(StockManufacturaDbContext dbContext)
        {
            var changed = false;

            var provider = dbContext.Proveedores.FirstOrDefault(p => p.Cuit == "30-77999111-3");
            if (provider is null)
            {
                provider = new Proveedor
                {
                    Nombre = "Botanica Aurora",
                    RazonSocial = "Botanica Aurora S.R.L.",
                    Cuit = "30-77999111-3",
                    Ciudad = "Mendoza",
                    Provincia = "Mendoza",
                    Pais = "Argentina",
                    Telefono = "+54 261 555-1020",
                    Email = "insumos@botanicaaurora.com",
                    PersonaContacto = "Lucia Ferrero",
                    Observaciones = "Proveedor de frascos, mechas y aditivos para velas de soja.",
                    Activo = true
                };
                dbContext.Proveedores.Add(provider);
                changed = true;
            }

            var ceraSoja = EnsureResource(dbContext, provider, "INS-SOY-WAX", "Cera de soja natural", "Insumo base para velas artesanales", "Cera", "kg", 165m, 40m, 12.5m, Moneda.ARS);
            var aceiteLavanda = EnsureResource(dbContext, provider, "INS-OIL-LAV", "Aceite esencial de lavanda (extracción propia)", "Aceite extraído en taller propio para línea relajante", "Aceite esencial", "ml", 9500m, 2500m, 0.38m, Moneda.ARS);
            var aceiteNaranja = EnsureResource(dbContext, provider, "INS-OIL-NAR", "Aceite esencial de naranja (extracción propia)", "Aceite cítrico extraído a baja temperatura", "Aceite esencial", "ml", 7800m, 2000m, 0.34m, Moneda.ARS);
            var mecha = EnsureResource(dbContext, provider, "INS-WICK-COT", "Mecha de algodón encerada", "Mecha para frasco 220ml", "Mecha", "unidad", 1400m, 300m, 35m, Moneda.ARS);
            var frasco = EnsureResource(dbContext, provider, "INS-JAR-220", "Frasco ámbar 220ml con tapa", "Envase premium para vela terminada", "Envase", "unidad", 900m, 200m, 210m, Moneda.ARS);

            changed = changed || ceraSoja.Changed || aceiteLavanda.Changed || aceiteNaranja.Changed || mecha.Changed || frasco.Changed;

            var velaLavanda = EnsureProduct(dbContext, "PRD-VELA-LAV-220", "Vela de soja Lavanda 220ml", "Vela artesanal de cera de soja con aceite esencial de lavanda", 1260m, 0.42m, 1789m);
            var velaCitrica = EnsureProduct(dbContext, "PRD-VELA-CIT-220", "Vela de soja Naranja cítrica 220ml", "Vela artesanal de soja con aceite de naranja de extracción propia", 1215m, 0.40m, 1701m);

            changed = changed || velaLavanda.Changed || velaCitrica.Changed;

            changed = EnsureRecipeItem(dbContext, velaLavanda.Product.Id, ceraSoja.Resource.Id, 0.18m, "Base de cera por unidad") || changed;
            changed = EnsureRecipeItem(dbContext, velaLavanda.Product.Id, aceiteLavanda.Resource.Id, 12m, "Fragancia principal") || changed;
            changed = EnsureRecipeItem(dbContext, velaLavanda.Product.Id, mecha.Resource.Id, 1m, "Mecha central") || changed;
            changed = EnsureRecipeItem(dbContext, velaLavanda.Product.Id, frasco.Resource.Id, 1m, "Envase final") || changed;

            changed = EnsureRecipeItem(dbContext, velaCitrica.Product.Id, ceraSoja.Resource.Id, 0.18m, "Base de cera por unidad") || changed;
            changed = EnsureRecipeItem(dbContext, velaCitrica.Product.Id, aceiteNaranja.Resource.Id, 11m, "Fragancia cítrica") || changed;
            changed = EnsureRecipeItem(dbContext, velaCitrica.Product.Id, mecha.Resource.Id, 1m, "Mecha central") || changed;
            changed = EnsureRecipeItem(dbContext, velaCitrica.Product.Id, frasco.Resource.Id, 1m, "Envase final") || changed;

            changed = EnsureOrder(dbContext, velaLavanda.Product.Id, "OP-DEMO-SOY-001", 80m, "Lote semanal lavanda", EstadoOrdenProduccion.Planificada) || changed;
            changed = EnsureOrder(dbContext, velaCitrica.Product.Id, "OP-DEMO-SOY-002", 60m, "Lote cítrico para feria", EstadoOrdenProduccion.EnProceso) || changed;
            changed = EnsureOrder(dbContext, velaLavanda.Product.Id, "OP-DEMO-SOY-003", 40m, "Pedido boutique centro", EstadoOrdenProduccion.Finalizada) || changed;

            return changed;
        }

        private static (Recurso Resource, bool Changed) EnsureResource(
            StockManufacturaDbContext dbContext,
            Proveedor provider,
            string code,
            string name,
            string description,
            string category,
            string unit,
            decimal stock,
            decimal minStock,
            decimal price,
            Moneda currency)
        {
            var resource = dbContext.Recursos.FirstOrDefault(r => r.Codigo == code);
            if (resource is not null)
            {
                return (resource, false);
            }

            resource = new Recurso
            {
                Codigo = code,
                Nombre = name,
                Descripcion = description,
                Categoria = category,
                UnidadMedida = unit,
                StockActual = stock,
                StockMinimo = minStock,
                Precio = price,
                Moneda = currency,
                ProveedorHabitualId = provider.Id,
                Observaciones = "Registro demo para pruebas funcionales.",
                Activo = true,
                FechaUltimaActualizacion = DateTime.UtcNow
            };

            dbContext.Recursos.Add(resource);
            return (resource, true);
        }

        private static (Producto Product, bool Changed) EnsureProduct(
            StockManufacturaDbContext dbContext,
            string code,
            string name,
            string description,
            decimal manufacturingCost,
            decimal margin,
            decimal suggestedPrice)
        {
            var product = dbContext.Productos.FirstOrDefault(p => p.Codigo == code);
            if (product is not null)
            {
                return (product, false);
            }

            product = new Producto
            {
                Codigo = code,
                Nombre = name,
                Descripcion = description,
                CostoFabricacionActual = manufacturingCost,
                MargenActual = margin,
                PrecioSugeridoActual = suggestedPrice,
                Activo = true,
                Observaciones = "Producto demo para pruebas de negocio de velas de soja.",
                FechaUltimoCalculo = DateTime.UtcNow
            };

            dbContext.Productos.Add(product);
            return (product, true);
        }

        private static bool EnsureRecipeItem(StockManufacturaDbContext dbContext, Guid productId, Guid resourceId, decimal quantity, string notes)
        {
            var exists = dbContext.RecetaProductoItems.Any(x => x.ProductoId == productId && x.RecursoId == resourceId);
            if (exists)
            {
                return false;
            }

            dbContext.RecetaProductoItems.Add(new RecetaProductoItem
            {
                ProductoId = productId,
                RecursoId = resourceId,
                Cantidad = quantity,
                CostoParcialManual = 0,
                Observaciones = notes
            });

            return true;
        }

        private static bool EnsureOrder(StockManufacturaDbContext dbContext, Guid productId, string code, decimal plannedQuantity, string notes, EstadoOrdenProduccion targetState)
        {
            if (dbContext.OrdenesProduccion.Any(o => o.Codigo == code))
            {
                return false;
            }

            var order = new OrdenProduccion(code, productId, plannedQuantity, notes);
            if (targetState == EstadoOrdenProduccion.Planificada)
            {
                order.Planificar();
            }
            else if (targetState == EstadoOrdenProduccion.EnProceso)
            {
                order.Planificar();
                order.MarcarEnProgreso();
            }
            else if (targetState == EstadoOrdenProduccion.Finalizada)
            {
                order.Planificar();
                order.MarcarEnProgreso();
                order.RegistrarProduccion(plannedQuantity);
                order.Completar();
            }

            dbContext.OrdenesProduccion.Add(order);
            return true;
        }

        private static string GetAdminPassword()
        {
            var configured = Environment.GetEnvironmentVariable("STOCKMANUFACTURA_ADMIN_PASSWORD");
            return string.IsNullOrWhiteSpace(configured) ? DefaultAdminPassword : configured;
        }
    }
}
