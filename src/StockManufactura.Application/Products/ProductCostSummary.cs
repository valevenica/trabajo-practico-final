namespace StockManufactura.Application.Products
{
    public sealed class ProductCostSummary
    {
        public Guid ProductId { get; init; }
        public string ProductName { get; init; } = string.Empty;
        public decimal CosteActual { get; init; }
        public decimal PrecioSugerido { get; init; }
        public decimal Margen { get; init; }
    }
}
