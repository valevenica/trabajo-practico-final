using System.Collections.Generic;

namespace StockManufactura.Web.Infrastructure
{
    public static class PermissionCatalog
    {
        public static readonly IReadOnlyList<(string Code, string Label)> All = new (string Code, string Label)[]
        {
            ("USUARIOS_ADMIN", "Administrar usuarios y roles"),
            ("PRODUCTOS_VER", "Ver productos"),
            ("PRODUCTOS_CREAR", "Crear productos"),
            ("PRODUCTOS_EDITAR", "Editar productos"),
            ("STOCK_VER", "Ver stock"),
            ("STOCK_AJUSTAR", "Ajustar stock"),
            ("COSTOS_VER", "Ver costos y precios"),
        };
    }
}
