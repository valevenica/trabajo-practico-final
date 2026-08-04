using System.Windows.Controls;
using StockManufactura.Desktop.Infrastructure;

namespace StockManufactura.Desktop.Views
{
    public partial class ProductionOrderManagementView : UserControl
    {
        public ProductionOrderManagementView()
        {
            InitializeComponent();

            var logo = DesktopAssetLoader.TryLoadLogoImage();
            if (logo is not null)
            {
                SidebarLogoImage.Source = logo;
            }
        }
    }
}
