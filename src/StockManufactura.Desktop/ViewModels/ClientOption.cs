namespace StockManufactura.Desktop.ViewModels
{
    /// <summary>Opción del selector de cliente de un pedido; <see cref="Id"/> nulo representa al consumidor final.</summary>
    public sealed record ClientOption(Guid? Id, string Nombre)
    {
        public const string ConsumidorFinal = "Consumidor final";

        public static ClientOption SinCliente { get; } = new(null, ConsumidorFinal);

        public override string ToString() => Nombre;
    }
}
