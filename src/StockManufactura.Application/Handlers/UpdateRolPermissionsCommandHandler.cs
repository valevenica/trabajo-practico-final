using System;
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
    public sealed class UpdateRolPermissionsCommandHandler : IRequestHandler<UpdateRolPermissionsCommand, RolDto>
    {
        private readonly IRepository<Rol> _rolRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public UpdateRolPermissionsCommandHandler(IRepository<Rol> rolRepository, IUnitOfWork unitOfWork, IMapper mapper)
        {
            _rolRepository = rolRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<RolDto> Handle(UpdateRolPermissionsCommand request, CancellationToken cancellationToken)
        {
            var rol = await _rolRepository.GetByIdAsync(request.Id);
            if (rol is null)
            {
                throw new KeyNotFoundException($"No se encontró el rol con id {request.Id}.");
            }

            rol.AsignarPermisos(request.Permisos ?? Array.Empty<string>());
            _rolRepository.Update(rol);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<RolDto>(rol);
        }
    }
}
