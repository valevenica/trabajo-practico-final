using System;

namespace StockManufactura.Desktop.Services
{
    public sealed class NavigationService
    {
        public Action<object>? NavigateAction { get; set; }

        public void NavigateTo(object viewModel)
        {
            if (viewModel is null)
            {
                throw new ArgumentNullException(nameof(viewModel));
            }

            NavigateAction?.Invoke(viewModel);
        }
    }
}
