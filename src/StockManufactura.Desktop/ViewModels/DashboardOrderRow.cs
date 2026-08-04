using System;
using System.Globalization;
using System.Windows.Media;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Desktop.ViewModels
{
    public sealed class DashboardOrderRow
    {
        public DashboardOrderRow(OrdenProduccion order, string productName, decimal estimatedCost)
        {
            Codigo = order.Codigo;
            ProductName = productName;
            Quantity = order.CantidadPlaneada.ToString("0.##", CultureInfo.InvariantCulture);
            Estado = GetStatusText(order.Estado);
            Fecha = GetDateText(order);
            Costo = FormatMoney(estimatedCost);
            (StatusBackground, StatusForeground) = GetStatusBrushes(order.Estado);
        }

        public string Codigo { get; }
        public string ProductName { get; }
        public string Quantity { get; }
        public string Estado { get; }
        public string Fecha { get; }
        public string Costo { get; }
        public Brush StatusBackground { get; }
        public Brush StatusForeground { get; }

        private static string GetStatusText(EstadoOrdenProduccion estado)
        {
            return estado switch
            {
                EstadoOrdenProduccion.Borrador => "BORRADOR",
                EstadoOrdenProduccion.Planificada => "PLANIFICADA",
                EstadoOrdenProduccion.EnProceso => "EN PROCESO",
                EstadoOrdenProduccion.Finalizada => "FINALIZADA",
                EstadoOrdenProduccion.Cancelada => "CANCELADA",
                _ => estado.ToString().ToUpperInvariant()
            };
        }

        private static string GetDateText(OrdenProduccion order)
        {
            var date = order.FechaFin ?? order.FechaInicio ?? order.CreatedAt;
            return date.ToString("dd/MM/yyyy");
        }

        private static string FormatMoney(decimal amount)
        {
            return $"${amount.ToString("N2", CultureInfo.InvariantCulture)}";
        }

        private static (Brush Background, Brush Foreground) GetStatusBrushes(EstadoOrdenProduccion estado)
        {
            return estado switch
            {
                EstadoOrdenProduccion.Finalizada => (new SolidColorBrush(Color.FromRgb(189, 239, 200)), new SolidColorBrush(Color.FromRgb(29, 101, 48))),
                EstadoOrdenProduccion.EnProceso => (new SolidColorBrush(Color.FromRgb(255, 226, 154)), new SolidColorBrush(Color.FromRgb(154, 109, 16))),
                EstadoOrdenProduccion.Cancelada => (new SolidColorBrush(Color.FromRgb(247, 200, 200)), new SolidColorBrush(Color.FromRgb(167, 38, 49))),
                EstadoOrdenProduccion.Planificada => (new SolidColorBrush(Color.FromRgb(230, 232, 240)), new SolidColorBrush(Color.FromRgb(70, 76, 102))),
                _ => (new SolidColorBrush(Color.FromRgb(244, 242, 238)), new SolidColorBrush(Color.FromRgb(85, 85, 85)))
            };
        }
    }
}