using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace StockManufactura.Desktop.Converters
{
    /// <summary>
    /// Colorea las alertas del tablero de decisiones segun su severidad.
    /// El parametro "fondo" devuelve la version suave para el relleno de la tarjeta.
    /// </summary>
    public sealed class SeverityToBrushConverter : IValueConverter
    {
        private static readonly SolidColorBrush CriticaTexto = new((Color)ColorConverter.ConvertFromString("#8C1D18"));
        private static readonly SolidColorBrush CriticaFondo = new((Color)ColorConverter.ConvertFromString("#FBE9E7"));
        private static readonly SolidColorBrush AdvertenciaTexto = new((Color)ColorConverter.ConvertFromString("#7A5300"));
        private static readonly SolidColorBrush AdvertenciaFondo = new((Color)ColorConverter.ConvertFromString("#FFF6E0"));
        private static readonly SolidColorBrush OkTexto = new((Color)ColorConverter.ConvertFromString("#1B5E20"));
        private static readonly SolidColorBrush OkFondo = new((Color)ColorConverter.ConvertFromString("#E8F5E9"));
        private static readonly SolidColorBrush NeutralTexto = new((Color)ColorConverter.ConvertFromString("#37343B"));
        private static readonly SolidColorBrush NeutralFondo = new((Color)ColorConverter.ConvertFromString("#F1EEE8"));

        static SeverityToBrushConverter()
        {
            foreach (var brush in new[] { CriticaTexto, CriticaFondo, AdvertenciaTexto, AdvertenciaFondo, OkTexto, OkFondo, NeutralTexto, NeutralFondo })
            {
                brush.Freeze();
            }
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var esFondo = string.Equals(parameter as string, "fondo", StringComparison.OrdinalIgnoreCase);

            return (value as string) switch
            {
                "Critica" => esFondo ? CriticaFondo : CriticaTexto,
                "Advertencia" => esFondo ? AdvertenciaFondo : AdvertenciaTexto,
                "Ok" => esFondo ? OkFondo : OkTexto,
                _ => esFondo ? NeutralFondo : NeutralTexto
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
