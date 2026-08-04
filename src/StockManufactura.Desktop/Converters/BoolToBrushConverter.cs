using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace StockManufactura.Desktop.Converters
{
    public sealed class BoolToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var isTrue = value is bool b && b;
            return isTrue ? Brushes.LightGreen : Brushes.LightCoral;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
