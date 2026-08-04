using MediatR;
using StockManufactura.Application.DTOs;

namespace StockManufactura.Application.Commands
{
    public sealed class CreateRolCommand : IRequest<RolDto>
    {
        public string Nombre { get; init; } = string.Empty;
        public string Descripcion { get; init; } = string.Empty;
    }
}
