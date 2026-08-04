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
    public sealed class CreateRolCommandHandler : IRequestHandler<CreateRolCommand, RolDto>
    {
        private readonly IRepository<Rol> _rolRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CreateRolCommandHandler(IRepository<Rol> rolRepository, IUnitOfWork unitOfWork, IMapper mapper)
        {
            _rolRepository = rolRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<RolDto> Handle(CreateRolCommand request, CancellationToken cancellationToken)
        {
            var rol = new Rol(request.Nombre, request.Descripcion);
            await _rolRepository.AddAsync(rol);
            await _unitOfWork.SaveChangesAsync();
            return _mapper.Map<RolDto>(rol);
        }
    }
}
