using StockManufactura.Domain.Entities;

namespace StockManufactura.Desktop.ViewModels
{
    public sealed class DashboardViewModel
    {
        public DashboardViewModel(Usuario usuario)
        {
            Usuario = usuario;
        }

        public Usuario Usuario { get; }
    }
}
