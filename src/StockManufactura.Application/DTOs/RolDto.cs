using System;

namespace StockManufactura.Application.DTOs
{
    public sealed class RolDto
    {
        public Guid Id { get; init; }
        public string Nombre { get; init; } = string.Empty;
        public string Descripcion { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
        public bool IsDeleted { get; init; }
    }
}
