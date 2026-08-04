using System.Collections.Generic;
using MediatR;
using StockManufactura.Application.DTOs;

namespace StockManufactura.Application.Commands
{
    public sealed class GetRolesQuery : IRequest<IEnumerable<RolDto>>
    {
    }
}
