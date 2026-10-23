using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Serilog;

namespace StockManufactura.Desktop.Infrastructure
{
    /// <summary>
    /// Punto unico de autorizacion para la navegacion entre modulos. Cada modulo declara los permisos
    /// que habilitan su ingreso (basta con uno); los modulos sin entrada son de acceso libre para
    /// cualquier usuario autenticado (panel principal y datos propios del usuario).
    /// </summary>
    internal static class ModuleAccess
    {
        public const string Dashboard = "Dashboard";
        public const string BusinessIntelligence = "BusinessIntelligence";
        public const string Shortages = "Shortages";
        public const string Products = "Products";
        public const string NewProduct = "NewProduct";
        public const string Resources = "Resources";
        public const string Providers = "Providers";
        public const string Clients = "Clients";
        public const string ProductionOrders = "ProductionOrders";
        public const string SpecialResources = "SpecialResources";
        public const string StockIntake = "StockIntake";
        public const string PriceImport = "PriceImport";
        public const string Monetary = "Monetary";
        public const string CostHistory = "CostHistory";
        public const string Audit = "Audit";
        public const string Backups = "Backups";

        private static readonly Dictionary<string, (string Nombre, string[] Permisos)> Modulos = new()
        {
            [BusinessIntelligence] = ("el tablero de inteligencia de negocio", new[] { "COSTOS_VER" }),
            [Shortages] = ("la pantalla de faltantes", new[] { "STOCK_VER" }),
            [Products] = ("la gestión de productos", new[] { "PRODUCTOS_VER", "PRODUCTOS_CREAR", "PRODUCTOS_EDITAR" }),
            [NewProduct] = ("el alta de productos", new[] { "PRODUCTOS_CREAR" }),
            [Resources] = ("la gestión de insumos", new[] { "PRODUCTOS_EDITAR" }),
            [Providers] = ("la gestión de proveedores", new[] { "PRODUCTOS_EDITAR", "USUARIOS_ADMIN" }),
            [Clients] = ("la gestión de clientes", new[] { "PRODUCTOS_EDITAR", "USUARIOS_ADMIN" }),
            [ProductionOrders] = ("las órdenes de producción", new[] { "PRODUCTOS_EDITAR" }),
            [SpecialResources] = ("los insumos especiales", new[] { "PRODUCTOS_EDITAR" }),
            [StockIntake] = ("los ingresos de stock", new[] { "STOCK_AJUSTAR" }),
            [PriceImport] = ("la carga masiva de precios", new[] { "PRODUCTOS_EDITAR" }),
            [Monetary] = ("la configuración monetaria", new[] { "COSTOS_VER" }),
            [CostHistory] = ("el historial de costos", new[] { "COSTOS_VER" }),
            [Audit] = ("la bitácora de auditoría", new[] { "USUARIOS_ADMIN" }),
            [Backups] = ("las copias de seguridad", new[] { "USUARIOS_ADMIN" }),
        };

        /// <summary>
        /// Mientras el usuario no reemplace una contraseña inicial o temporal solo puede usar "Mi cuenta".
        /// </summary>
        public static bool PasswordChangePending => AuthSession.Current?.Usuario.RequiereCambioPassword == true;

        public static bool CanEnter(string modulo)
        {
            var session = AuthSession.Current;
            if (session is null)
            {
                return false;
            }

            if (PasswordChangePending)
            {
                // Con el cambio pendiente solo se puede ver la pantalla principal (que lo avisa) y "Mi cuenta".
                return modulo == Dashboard;
            }

            return !Modulos.TryGetValue(modulo, out var regla) || regla.Permisos.Any(session.TienePermiso);
        }

        /// <summary>
        /// Verifica el permiso del modulo e informa al usuario cuando no lo tiene.
        /// </summary>
        public static bool TryEnter(string modulo)
        {
            if (CanEnter(modulo))
            {
                return true;
            }

            if (PasswordChangePending)
            {
                MessageBox.Show(
                    "Debe cambiar su contraseña antes de usar el resto del sistema. Use el botón «Cambiar contraseña» de la pantalla principal.",
                    "Cambio de contraseña pendiente",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return false;
            }

            var nombre = Modulos.TryGetValue(modulo, out var regla) ? regla.Nombre : "este módulo";
            Log.Warning("Acceso denegado al modulo {Modulo} para {Usuario}", modulo, AuthSession.Current?.Usuario.Email);
            MessageBox.Show(
                $"No tiene permisos para acceder a {nombre}. Solicite al administrador que asigne el permiso correspondiente a su rol.",
                "Acceso denegado",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return false;
        }
    }
}
