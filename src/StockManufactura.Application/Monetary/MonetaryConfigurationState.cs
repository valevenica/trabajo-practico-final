using System;

namespace StockManufactura.Application.Monetary
{
    public sealed class MonetaryConfigurationState
    {
        public decimal CurrentRate { get; set; }
        public DateTime LastUpdate { get; set; }
        public string Source { get; set; } = string.Empty;
    }
}
