using AutoMapper;
using StockManufactura.Application.DTOs;
using StockManufactura.Application.Interfaces;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Application.Services
{
    public sealed class UserManagementService : IUserManagementService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IAuditLogService _auditLogService;

        public UserManagementService(IUnitOfWork unitOfWork, IMapper mapper, IAuditLogService auditLogService)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        }

        public async Task<IReadOnlyList<UsuarioDto>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var users = await _unitOfWork.Usuarios.ListWithRoleAsync();
            return users.Select(u => _mapper.Map<UsuarioDto>(u)).ToList();
        }

        public async Task<IReadOnlyList<Rol>> GetRolesAsync(CancellationToken cancellationToken = default)
        {
            var roles = await _unitOfWork.Roles.ListAsync();
            return roles.OrderBy(r => r.Nombre).ToList();
        }

        public async Task<UsuarioDto> CreateAsync(UpsertUsuarioRequest request, string plainPassword, string actor, CancellationToken cancellationToken = default)
        {
            ValidatePassword(plainPassword);
            var normalizedEmail = NormalizeEmail(request.Email);

            var existingUser = await _unitOfWork.Usuarios.GetByEmailAsync(normalizedEmail);
            if (existingUser is not null)
            {
                throw new InvalidOperationException("Ya existe un usuario con ese email.");
            }

            var rol = await _unitOfWork.Roles.GetByIdAsync(request.RolId);
            if (rol is null)
            {
                throw new InvalidOperationException("Rol no encontrado.");
            }

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(plainPassword);
            var usuario = new Usuario(request.Nombre.Trim(), normalizedEmail, passwordHash, request.RolId);
            usuario.AsignarRol(rol);
            if (!request.EsActivo)
            {
                usuario.Desactivar();
            }
            usuario.ForzarCambioPassword();

            await _unitOfWork.Usuarios.AddAsync(usuario);
            await _unitOfWork.SaveChangesAsync();

            await RegisterAuditAsync(actor, "Crear", usuario, $"Alta de usuario {usuario.Email}", cancellationToken);
            return _mapper.Map<UsuarioDto>(usuario) ?? throw new InvalidOperationException("No se pudo mapear el usuario creado.");
        }

        public async Task<UsuarioDto> UpdateAsync(Guid userId, UpsertUsuarioRequest request, string actor, CancellationToken cancellationToken = default)
        {
            var usuario = await _unitOfWork.Usuarios.GetByIdWithRoleAsync(userId);
            if (usuario is null)
            {
                throw new InvalidOperationException("Usuario no encontrado.");
            }

            var rol = await _unitOfWork.Roles.GetByIdAsync(request.RolId);
            if (rol is null)
            {
                throw new InvalidOperationException("Rol no encontrado.");
            }

            var normalizedEmail = NormalizeEmail(request.Email);
            var existingUser = await _unitOfWork.Usuarios.GetByEmailAsync(normalizedEmail);
            if (existingUser is not null && existingUser.Id != usuario.Id)
            {
                throw new InvalidOperationException("Ya existe un usuario con ese email.");
            }

            usuario.ActualizarDatos(request.Nombre.Trim(), normalizedEmail);
            usuario.AsignarRol(rol);
            if (request.EsActivo)
            {
                usuario.Activar();
            }
            else
            {
                usuario.Desactivar();
            }

            _unitOfWork.Usuarios.Update(usuario);
            await _unitOfWork.SaveChangesAsync();
            await RegisterAuditAsync(actor, "Editar", usuario, $"Edición de usuario {usuario.Email}", cancellationToken);
            return _mapper.Map<UsuarioDto>(usuario) ?? throw new InvalidOperationException("No se pudo mapear el usuario actualizado.");
        }

        public async Task<UsuarioDto> SetActiveAsync(Guid userId, bool active, string actor, CancellationToken cancellationToken = default)
        {
            var usuario = await _unitOfWork.Usuarios.GetByIdWithRoleAsync(userId);
            if (usuario is null)
            {
                throw new InvalidOperationException("Usuario no encontrado.");
            }

            if (active)
            {
                usuario.Activar();
            }
            else
            {
                usuario.Desactivar();
            }

            _unitOfWork.Usuarios.Update(usuario);
            await _unitOfWork.SaveChangesAsync();
            await RegisterAuditAsync(actor, active ? "Activar" : "Desactivar", usuario, $"{(active ? "Activación" : "Desactivación")} de usuario {usuario.Email}", cancellationToken);
            return _mapper.Map<UsuarioDto>(usuario) ?? throw new InvalidOperationException("No se pudo mapear el estado actualizado del usuario.");
        }

        public async Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, string actor, CancellationToken cancellationToken = default)
        {
            ValidatePassword(newPassword);

            var usuario = await _unitOfWork.Usuarios.GetByIdAsync(userId);
            if (usuario is null)
            {
                throw new InvalidOperationException("Usuario no encontrado.");
            }

            if (!BCrypt.Net.BCrypt.Verify(currentPassword, usuario.PasswordHash))
            {
                throw new InvalidOperationException("La contraseña actual es inválida.");
            }

            usuario.CambiarPasswordHash(BCrypt.Net.BCrypt.HashPassword(newPassword));
            _unitOfWork.Usuarios.Update(usuario);
            await _unitOfWork.SaveChangesAsync();
            await RegisterAuditAsync(actor, "CambiarPassword", usuario, $"Cambio de contraseña para {usuario.Email}", cancellationToken);
        }

        public async Task ResetPasswordAsync(Guid userId, string newPassword, string actor, CancellationToken cancellationToken = default)
        {
            ValidatePassword(newPassword);

            var usuario = await _unitOfWork.Usuarios.GetByIdAsync(userId);
            if (usuario is null)
            {
                throw new InvalidOperationException("Usuario no encontrado.");
            }

            usuario.CambiarPasswordHash(BCrypt.Net.BCrypt.HashPassword(newPassword));
            _unitOfWork.Usuarios.Update(usuario);
            await _unitOfWork.SaveChangesAsync();
            await RegisterAuditAsync(actor, "CambiarPassword", usuario, $"Cambio de contraseña para {usuario.Email}", cancellationToken);
        }

        public async Task<Rol> CreateRoleAsync(string nombre, string descripcion, string actor, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(nombre))
            {
                throw new InvalidOperationException("El nombre del rol es requerido.");
            }

            var existingRole = (await _unitOfWork.Roles.ListAsync())
                .FirstOrDefault(r => string.Equals(r.Nombre, nombre.Trim(), StringComparison.OrdinalIgnoreCase));
            if (existingRole is not null)
            {
                throw new InvalidOperationException("Ya existe un rol con ese nombre.");
            }

            var rol = new Rol(nombre.Trim(), descripcion?.Trim() ?? string.Empty);
            await _unitOfWork.Roles.AddAsync(rol);
            await _unitOfWork.SaveChangesAsync();

            await RegisterRoleAuditAsync(actor, "Crear", rol, $"Alta de rol {rol.Nombre}", cancellationToken);
            return rol;
        }

        public async Task<Rol> UpdateRolePermissionsAsync(Guid rolId, IReadOnlyCollection<string> permisos, string actor, CancellationToken cancellationToken = default)
        {
            var rol = await _unitOfWork.Roles.GetByIdAsync(rolId);
            if (rol is null)
            {
                throw new InvalidOperationException("Rol no encontrado.");
            }

            rol.AsignarPermisos(permisos ?? Array.Empty<string>());
            _unitOfWork.Roles.Update(rol);
            await _unitOfWork.SaveChangesAsync();

            await RegisterRoleAuditAsync(actor, "EditarPermisos", rol, $"Actualización de permisos del rol {rol.Nombre}", cancellationToken);
            return rol;
        }

        private Task RegisterRoleAuditAsync(string actor, string action, Rol rol, string description, CancellationToken cancellationToken)
        {
            return _auditLogService.RegisterAsync(new AuditLog
            {
                Usuario = string.IsNullOrWhiteSpace(actor) ? "system" : actor,
                Modulo = "Roles",
                Accion = action,
                Entidad = "Rol",
                IdEntidad = rol.Id.ToString(),
                Descripcion = description,
                Equipo = Environment.MachineName
            }, cancellationToken);
        }

        private static void ValidatePassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
            {
                throw new InvalidOperationException("La contraseña debe tener al menos 8 caracteres.");
            }
        }

        private static string NormalizeEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new InvalidOperationException("Email requerido.");
            }

            return email.Trim().ToLowerInvariant();
        }

        private Task RegisterAuditAsync(string actor, string action, Usuario user, string description, CancellationToken cancellationToken)
        {
            return _auditLogService.RegisterAsync(new AuditLog
            {
                Usuario = string.IsNullOrWhiteSpace(actor) ? "system" : actor,
                Modulo = "Usuarios",
                Accion = action,
                Entidad = "Usuario",
                IdEntidad = user.Id.ToString(),
                Descripcion = description,
                Equipo = Environment.MachineName
            }, cancellationToken);
        }
    }
}
