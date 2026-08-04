using System;
using MediatR;
using StockManufactura.Application.DTOs;

namespace StockManufactura.Application.Commands
{
    public sealed class CreateUsuarioCommand : IRequest<UsuarioDto>
    {
        public string Nombre { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
        public Guid RolId { get; init; }
    }
}
