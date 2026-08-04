using System;

namespace StockManufactura.Application.DTOs
{
    public sealed class UsuarioDto
    {
        public Guid Id { get; init; }
        public string Nombre { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public Guid RolId { get; init; }
        public string RolNombre { get; init; } = string.Empty;
        public bool EsActivo { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
        public bool IsDeleted { get; init; }
    }
}
