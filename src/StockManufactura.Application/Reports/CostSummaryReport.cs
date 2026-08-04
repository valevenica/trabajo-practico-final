namespace StockManufactura.Application.Reports
{
    public sealed class CostSummaryReport
    {
        public int TotalProductos { get; init; }
        public int TotalRecetas { get; init; }
        public decimal CostoPromedio { get; init; }
        public decimal CostoMaximo { get; init; }
        public decimal CostoMinimo { get; init; }
    }
}
