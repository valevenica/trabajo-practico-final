using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace StockManufactura.Desktop.Converters
{
    /// <summary>
    /// Traduce la escala normalizada de un punto de serie (0 a 1) en anchos proporcionales
    /// de una grilla de dos columnas, para dibujar barras sin depender de una libreria de graficos.
    /// El parametro "resto" devuelve la porcion vacia de la barra.
    /// </summary>
    public sealed class ScaleToGridLengthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var escala = value switch
            {
                double d => d,
                float f => f,
                decimal m => (double)m,
                _ => 0d
            };

            if (double.IsNaN(escala) || double.IsInfinity(escala) || escala < 0d)
            {
                escala = 0d;
            }
            else if (escala > 1d)
            {
                escala = 1d;
            }

            var esResto = string.Equals(parameter as string, "resto", StringComparison.OrdinalIgnoreCase);
            return new GridLength(esResto ? 1d - escala : escala, GridUnitType.Star);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
