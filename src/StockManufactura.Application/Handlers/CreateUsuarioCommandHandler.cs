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
    public sealed class CreateUsuarioCommandHandler : IRequestHandler<CreateUsuarioCommand, UsuarioDto>
    {
        private readonly IRepository<Usuario> _usuarioRepository;
        private readonly IRepository<Rol> _rolRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CreateUsuarioCommandHandler(
            IRepository<Usuario> usuarioRepository,
            IRepository<Rol> rolRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _usuarioRepository = usuarioRepository;
            _rolRepository = rolRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<UsuarioDto> Handle(CreateUsuarioCommand request, CancellationToken cancellationToken)
        {
            var rol = await _rolRepository.GetByIdAsync(request.RolId);
            if (rol is null)
            {
                throw new InvalidOperationException("Rol no encontrado.");
            }

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            var usuario = new Usuario(request.Nombre, request.Email, passwordHash, request.RolId);
            usuario.AsignarRol(rol);
            await _usuarioRepository.AddAsync(usuario);
            await _unitOfWork.SaveChangesAsync();
            return _mapper.Map<UsuarioDto>(usuario);
        }
    }
}
