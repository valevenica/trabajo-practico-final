using System.Globalization;

namespace StockManufactura.Web.Infrastructure
{
    /// <summary>Período mensual de los filtros; sin año ni mes abarca toda la historia.</summary>
    public sealed record Periodo(string Clave, int? Year, int? Month, string Etiqueta);

    public static class Periodos
    {
        private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-AR");

        /// <summary>La opción "todo" seguida de los últimos doce meses, del más reciente al más antiguo.</summary>
        public static IReadOnlyList<Periodo> UltimosDoceMeses(string etiquetaTodo)
        {
            var lista = new List<Periodo> { new("todo", null, null, etiquetaTodo) };
            var cursor = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            for (var i = 0; i < 12; i++)
            {
                var etiqueta = cursor.ToString("MMMM yyyy", Cultura);
                lista.Add(new($"{cursor:yyyy-MM}", cursor.Year, cursor.Month, char.ToUpperInvariant(etiqueta[0]) + etiqueta[1..]));
                cursor = cursor.AddMonths(-1);
            }

            return lista;
        }

        public static string MesActual => $"{DateTime.UtcNow:yyyy-MM}";
    }
}
