using System;
using System.Globalization;
using System.Windows.Data;

namespace StockManufactura.Desktop.Converters
{
    /// <summary>
    /// Las fechas se guardan en UTC; en pantalla se muestran en la hora local del equipo. Devuelve un DateTime para que el
    /// StringFormat de la columna siga aplicándose.
    /// </summary>
    public sealed class UtcToLocalConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is DateTime fecha
                ? DateTime.SpecifyKind(fecha, DateTimeKind.Utc).ToLocalTime()
                : value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
