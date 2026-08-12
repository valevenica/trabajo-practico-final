using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using StockManufactura.Application.Resources;

namespace StockManufactura.Desktop.Converters
{
    public sealed class ResourcePriceImportStatusToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value switch
            {
                ResourcePriceImportRowStatus.Ok => Brushes.LightGreen,
                ResourcePriceImportRowStatus.SinCambios => Brushes.WhiteSmoke,
                ResourcePriceImportRowStatus.NoEncontrado => Brushes.LightCoral,
                ResourcePriceImportRowStatus.PrecioInvalido => Brushes.Khaki,
                _ => Brushes.White
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
