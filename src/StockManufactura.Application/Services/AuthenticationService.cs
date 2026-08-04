using System;
using System.Threading;
using System.Threading.Tasks;
using StockManufactura.Application.DTOs;
using StockManufactura.Application.Interfaces;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Application.Services
{
    public sealed class AuthenticationService : IAuthenticationService
    {
        private const int MaxFailedAttempts = 5;
        private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);
        private readonly IUnitOfWork _unitOfWork;

        public AuthenticationService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<AuthenticationResult> AuthenticateDetailedAsync(string email, string password, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                return AuthenticationResult.Failed("Usuario y contraseña son requeridos.");
            }

            var usuario = await _unitOfWork.Usuarios.GetByEmailAsync(email, includeRole: true);
            if (usuario is null || !usuario.EsActivo)
            {
                return AuthenticationResult.Failed("Credenciales inválidas.");
            }

            var now = DateTime.UtcNow;
            if (usuario.EstaBloqueado(now))
            {
                return new AuthenticationResult
                {
                    IsLockedOut = true,
                    IsSuccess = false,
                    LockoutEndUtc = usuario.BloqueadoHastaUtc,
                    Message = "Usuario bloqueado temporalmente por intentos fallidos."
                };
            }

            if (!BCrypt.Net.BCrypt.Verify(password, usuario.PasswordHash))
            {
                usuario.RegistrarIntentoFallido(now, MaxFailedAttempts, LockoutDuration);
                _unitOfWork.Usuarios.Update(usuario);
                await _unitOfWork.SaveChangesAsync();

                return AuthenticationResult.Failed("Credenciales inválidas.");
            }

            return new AuthenticationResult
            {
                IsSuccess = true,
                RequiresPasswordChange = usuario.RequiereCambioPassword,
                Usuario = usuario,
                Message = usuario.RequiereCambioPassword
                    ? "Debe cambiar su contraseña para continuar."
                    : "Ingreso exitoso."
            };
        }

        public async Task<Usuario?> AuthenticateAsync(string email, string password, CancellationToken cancellationToken = default)
        {
            var result = await AuthenticateDetailedAsync(email, password, cancellationToken);
            return result.IsSuccess ? result.Usuario : null;
        }

        public async Task<Usuario> RegisterLoginAsync(Usuario usuario, CancellationToken cancellationToken = default)
        {
            usuario.RegistrarAcceso();
            _unitOfWork.Usuarios.Update(usuario);
            await _unitOfWork.SaveChangesAsync();
            return usuario;
        }
    }
}
