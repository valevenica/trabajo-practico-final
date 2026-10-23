using System;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using StockManufactura.Application.Email;
using StockManufactura.Application.Interfaces;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Application.Services
{
    public sealed class PasswordRecoveryService : IPasswordRecoveryService
    {
        private const int LongitudMinimaPassword = 8;

        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailSettingsStore _settingsStore;
        private readonly IEmailSender _emailSender;
        private readonly PasswordRecoveryCodeStore _codeStore;
        private readonly IAuditLogService _auditLogService;

        public PasswordRecoveryService(
            IUnitOfWork unitOfWork,
            IEmailSettingsStore settingsStore,
            IEmailSender emailSender,
            PasswordRecoveryCodeStore codeStore,
            IAuditLogService auditLogService)
        {
            _unitOfWork = unitOfWork;
            _settingsStore = settingsStore;
            _emailSender = emailSender;
            _codeStore = codeStore;
            _auditLogService = auditLogService;
        }

        public Task<EmailSettings> GetEmailSettingsAsync(CancellationToken cancellationToken = default)
            => _settingsStore.LoadAsync(cancellationToken);

        public async Task SaveEmailSettingsAsync(EmailSettings settings, string actor, CancellationToken cancellationToken = default)
        {
            if (!settings.EstaCompleta)
            {
                throw new InvalidOperationException("Debe indicar el servidor SMTP, el puerto y el correo remitente.");
            }

            await _settingsStore.SaveAsync(settings, cancellationToken);
            await RegisterAuditAsync(actor, "ConfigurarCorreo", string.Empty, $"Cuenta de correo saliente configurada: {settings.Remitente} vía {settings.Host}:{settings.Puerto}", cancellationToken);
        }

        public async Task SendTestEmailAsync(string destinatario, CancellationToken cancellationToken = default)
        {
            var settings = await GetConfiguredSettingsAsync(cancellationToken);
            await _emailSender.SendAsync(
                settings,
                destinatario,
                "Integra Manufacturing - Correo de prueba",
                "Este es un correo de prueba. La cuenta de correo saliente del sistema quedó configurada correctamente.",
                cancellationToken);
        }

        public async Task RequestCodeAsync(string email, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new InvalidOperationException("Ingrese el correo de su perfil.");
            }

            var settings = await GetConfiguredSettingsAsync(cancellationToken);
            var usuario = await _unitOfWork.Usuarios.GetByEmailAsync(email.Trim());
            if (usuario is null || !usuario.EsActivo)
            {
                // Misma respuesta que en el caso exitoso: no se revela si la cuenta existe.
                Log.Information("Solicitud de recuperacion para un correo sin usuario activo: {Email}", email);
                return;
            }

            var espera = _codeStore.TiempoRestanteParaReenviar(usuario.Id);
            if (espera.HasValue)
            {
                throw new InvalidOperationException($"Ya se envió un código. Espere {Math.Ceiling(espera.Value.TotalSeconds)} segundos para pedir otro.");
            }

            var codigo = _codeStore.Generar(usuario.Id);
            var minutos = (int)PasswordRecoveryCodeStore.Vigencia.TotalMinutes;
            var cuerpo =
                $"Hola {usuario.Nombre}:{Environment.NewLine}{Environment.NewLine}" +
                $"Su código para restablecer la contraseña de Integra Manufacturing es: {codigo}{Environment.NewLine}{Environment.NewLine}" +
                $"Su nombre de usuario para ingresar al sistema es: {usuario.Nombre}{Environment.NewLine}{Environment.NewLine}" +
                $"El código vence en {minutos} minutos y solo puede usarse una vez. Si usted no lo solicitó, ignore este correo; su contraseña no cambiará.";

            try
            {
                await _emailSender.SendAsync(settings, usuario.Email, "Integra Manufacturing - Código de recuperación", cuerpo, cancellationToken);
            }
            catch (Exception ex)
            {
                _codeStore.Descartar(usuario.Id);
                Log.Error(ex, "No se pudo enviar el codigo de recuperacion a {Email}", usuario.Email);
                throw new InvalidOperationException("No se pudo enviar el correo. Verifique la conexión a Internet o contacte al administrador.", ex);
            }

            await RegisterAuditAsync(usuario.Email, "RecuperacionSolicitada", usuario.Id.ToString(), "Se envió un código de recuperación al correo del perfil.", cancellationToken);
        }

        public async Task<string> ResetWithCodeAsync(string email, string codigo, string nuevaPassword, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(nuevaPassword) || nuevaPassword.Length < LongitudMinimaPassword)
            {
                throw new InvalidOperationException($"La nueva contraseña debe tener al menos {LongitudMinimaPassword} caracteres.");
            }

            var usuario = string.IsNullOrWhiteSpace(email) ? null : await _unitOfWork.Usuarios.GetByEmailAsync(email.Trim());
            if (usuario is null || !usuario.EsActivo)
            {
                throw new InvalidOperationException("El código es incorrecto o venció. Solicite uno nuevo.");
            }

            switch (_codeStore.Validar(usuario.Id, codigo))
            {
                case CodeValidation.Valido:
                    break;
                case CodeValidation.Incorrecto:
                    throw new InvalidOperationException("El código es incorrecto.");
                case CodeValidation.IntentosAgotados:
                    await RegisterAuditAsync(usuario.Email, "RecuperacionFallida", usuario.Id.ToString(), "Se agotaron los intentos de ingreso del código de recuperación.", cancellationToken);
                    throw new InvalidOperationException("Se agotaron los intentos. Solicite un código nuevo.");
                default:
                    throw new InvalidOperationException("El código es incorrecto o venció. Solicite uno nuevo.");
            }

            // CambiarPasswordHash tambien reinicia los intentos fallidos y levanta el bloqueo de la cuenta.
            usuario.CambiarPasswordHash(BCrypt.Net.BCrypt.HashPassword(nuevaPassword));
            _unitOfWork.Usuarios.Update(usuario);
            await _unitOfWork.SaveChangesAsync();
            await RegisterAuditAsync(usuario.Email, "RecuperacionPassword", usuario.Id.ToString(), "Contraseña restablecida mediante código enviado por correo.", cancellationToken);
            return usuario.Nombre;
        }

        private async Task<EmailSettings> GetConfiguredSettingsAsync(CancellationToken cancellationToken)
        {
            var settings = await _settingsStore.LoadAsync(cancellationToken);
            if (!settings.EstaCompleta)
            {
                throw new InvalidOperationException("La recuperación por correo no está configurada. Solicite al administrador que restablezca su contraseña.");
            }

            return settings;
        }

        private Task RegisterAuditAsync(string usuario, string accion, string idEntidad, string descripcion, CancellationToken cancellationToken)
        {
            return _auditLogService.RegisterAsync(new AuditLog
            {
                Usuario = string.IsNullOrWhiteSpace(usuario) ? "anonymous" : usuario,
                Modulo = "Autenticacion",
                Accion = accion,
                Entidad = "Usuario",
                IdEntidad = idEntidad,
                Descripcion = descripcion,
                Equipo = Environment.MachineName
            }, cancellationToken);
        }
    }
}
