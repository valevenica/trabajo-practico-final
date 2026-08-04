namespace StockManufactura.Application.Reports
{
    public sealed class CostImpactReportItem
    {
        public string Producto { get; set; } = string.Empty;
        public decimal CostoAnterior { get; set; }
        public decimal CostoNuevo { get; set; }
        public decimal VariacionAbsoluta { get; set; }
        public decimal VariacionPorcentual { get; set; }
    }
}
