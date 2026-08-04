namespace StockManufactura.Application.DTOs
{
    public sealed class UpsertUsuarioRequest
    {
        public string Nombre { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public Guid RolId { get; init; }
        public bool EsActivo { get; init; } = true;
    }
}
