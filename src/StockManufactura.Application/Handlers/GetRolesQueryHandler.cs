using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using StockManufactura.Application.Commands;
using StockManufactura.Application.DTOs;
using StockManufactura.Application.Interfaces;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Application.Handlers
{
    public sealed class GetRolesQueryHandler : IRequestHandler<GetRolesQuery, IEnumerable<RolDto>>
    {
        private readonly IRepository<Rol> _rolRepository;
        private readonly IMapper _mapper;

        public GetRolesQueryHandler(IRepository<Rol> rolRepository, IMapper mapper)
        {
            _rolRepository = rolRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<RolDto>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
        {
            var roles = await _rolRepository.ListAsync();
            return _mapper.Map<IEnumerable<RolDto>>(roles);
        }
    }
}
