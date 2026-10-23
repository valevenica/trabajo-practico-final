using System;
using System.Collections;
using System.Globalization;
using System.Windows.Data;

namespace StockManufactura.Desktop.Converters
{
    /// <summary>
    /// Muestra el nombre de una entidad a partir de su identificador: values[0] es el Id (Guid o Guid?) y values[1] la
    /// colección donde buscarlo (elementos con propiedades Id y Nombre). El parámetro es el texto para un Id nulo.
    /// </summary>
    public sealed class EntityNameConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 || values[0] is not Guid id)
            {
                return parameter as string ?? string.Empty;
            }

            if (values[1] is IEnumerable items)
            {
                foreach (var item in items)
                {
                    var type = item?.GetType();
                    if (type?.GetProperty("Id")?.GetValue(item) is Guid itemId && itemId == id)
                    {
                        return type.GetProperty("Nombre")?.GetValue(item) as string ?? string.Empty;
                    }
                }
            }

            return "(no disponible)";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
