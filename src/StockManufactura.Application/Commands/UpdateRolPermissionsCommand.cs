using System;
using System.Collections.Generic;
using MediatR;
using StockManufactura.Application.DTOs;

namespace StockManufactura.Application.Commands
{
    public sealed class UpdateRolPermissionsCommand : IRequest<RolDto>
    {
        public Guid Id { get; set; }
        public IReadOnlyCollection<string>? Permisos { get; set; }
    }
}
